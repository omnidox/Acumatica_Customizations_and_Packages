<#
Read-only test: can Acumatica's DAC-based OData return the objects that Velixo ACU.QUERY refuses?

Run it yourself (it prompts for your Acumatica login; nothing is saved):
    powershell -ExecutionPolicy Bypass -File .\Test-DacOData.ps1

Output (no credentials, no auth headers) goes to .\OData_Test_Results\ :
    metadata.xml          Acumatica's list of OData entity sets / types
    entitysets.csv        entity set name -> entity type, for the target DACs
    <Object>.json         rows returned for the sample (kit SSS575074013010 etc.)
    summary.csv           per object: URL tried, HTTP status, rows, check vs SQL snapshot
Only GET requests are sent. The session is signed out at the end.
#>
param(
    [string]$BaseUrl = 'http://localhost:8888/AcumaticaERP',
    [string]$Tenant  = 'SnapshotTest',
    [System.Management.Automation.PSCredential]$Credential
)

$ErrorActionPreference = 'Stop'
$outDir = Join-Path $PSScriptRoot 'OData_Test_Results'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$root = "$BaseUrl/t/$Tenant/api/odata/dac"

$cred = if ($Credential) { $Credential } else { Get-Credential -Message "Acumatica login for $BaseUrl (tenant $Tenant). Used only in memory for this test." }
if (-not $cred) { Write-Host 'Cancelled.'; return }
$pair = '{0}:{1}' -f $cred.UserName, $cred.GetNetworkCredential().Password
$headers = @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pair)); Accept = 'application/json' }
$pair = $null; $cred = $null
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-ErrorBody($err) {
    try {
        $resp = $err.Exception.Response
        if (-not $resp) { return $err.Exception.Message }
        $sr = New-Object IO.StreamReader($resp.GetResponseStream())
        $body = $sr.ReadToEnd()
        $msg = 'HTTP {0}: {1}' -f [int]$resp.StatusCode, ($body -replace '\s+', ' ')
        return $msg.Substring(0, [Math]::Min(400, $msg.Length))
    } catch { return $err.Exception.Message }
}

# ---- 1. metadata: which entity sets exist for the target DACs
$targets = 'INKitSpecHdr','INKitSpecStkDet','INSiteStatus','INSiteStatusByCostCenter','SOLine','POLine','POVendorInventory','SOOrder'
$sets = @()
try {
    $meta = Invoke-WebRequest -Uri "$root/`$metadata" -Headers @{ Authorization = $headers.Authorization } -WebSession $session -UseBasicParsing
    [IO.File]::WriteAllText((Join-Path $outDir 'metadata.xml'), $meta.Content)
    [xml]$x = $meta.Content
    $sets = $x.SelectNodes("//*[local-name()='EntitySet']") | ForEach-Object { [pscustomobject]@{ EntitySet = $_.Name; EntityType = $_.EntityType } }
    $sets | Where-Object { $n = $_; $targets | Where-Object { $n.EntitySet -match "^$_\d*$" -or $n.EntityType -match "[._]$_$" } } |
        Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $outDir 'entitysets.csv')
    Write-Host ("metadata OK: {0} entity sets" -f @($sets).Count)
} catch { Write-Host ('metadata FAILED: ' + (Get-ErrorBody $_)) }

# ---- 2. sample queries (same filters as the Velixo workbook)
$items = 'InventoryID eq 364331 or InventoryID eq 344709 or InventoryID eq 363555 or InventoryID eq 363490 or InventoryID eq 363567'
$comps = '(InventoryID eq 344709 or InventoryID eq 363555 or InventoryID eq 363490 or InventoryID eq 363567)'
$tests = @(
  @{ Obj='INKitSpecHdr';             Full='PX.Objects.IN.INKitSpecHdr';             Filter='KitInventoryID eq 364331'; Select='KitInventoryID,RevisionID,IsActive,Descr'; ExpRows=1; SumField=$null; ExpSum=$null },
  @{ Obj='INKitSpecStkDet';          Full='PX.Objects.IN.INKitSpecStkDet';          Filter='KitInventoryID eq 364331'; Select='KitInventoryID,RevisionID,LineNbr,CompInventoryID,DfltCompQty,UOM'; ExpRows=4; SumField='DfltCompQty'; ExpSum=4 },
  @{ Obj='INSiteStatus';             Full='PX.Objects.IN.INSiteStatus';             Filter="($items) and SiteID eq 277"; Select='InventoryID,SiteID,QtyOnHand,QtyAvail,QtySOBooked,QtyPOOrders,QtyPOPrepared'; ExpRows=5; SumField='QtyOnHand'; ExpSum=5024 },
  @{ Obj='INSiteStatusByCostCenter'; Full='PX.Objects.IN.INSiteStatusByCostCenter'; Filter="($items) and SiteID eq 277"; Select='InventoryID,SiteID,CostCenterID,QtyOnHand,QtyAvail,QtySOBooked,QtyPOOrders,QtyPOPrepared'; ExpRows=5; SumField='QtyOnHand'; ExpSum=5024 },
  @{ Obj='SOLine';                   Full='PX.Objects.SO.SOLine';                   Filter="InventoryID eq 364331 and Operation eq 'I' and Completed eq false and OpenQty gt 0"; Select='OrderType,OrderNbr,LineNbr,CustomerID,InventoryID,RequestDate,OrderQty,ShippedQty,OpenQty'; ExpRows=3; SumField='OpenQty'; ExpSum=577 },
  @{ Obj='POLine';                   Full='PX.Objects.PO.POLine';                   Filter="$comps and Completed eq false and Cancelled eq false and OpenQty gt 0"; Select='OrderType,OrderNbr,LineNbr,VendorID,InventoryID,OrderQty,OpenQty,PromisedDate'; ExpRows=2; SumField='OpenQty'; ExpSum=1920 },
  @{ Obj='POVendorInventory';        Full='PX.Objects.PO.POVendorInventory';        Filter=$comps; Select='InventoryID,VendorID,VendorLocationID,PurchaseUnit,MinOrdQty,LotSize,AddLeadTimeDays,Active'; ExpRows=5; SumField='MinOrdQty'; ExpSum=0 },
  @{ Obj='SOOrder (control)';        Full='PX.Objects.SO.SOOrder';                  Filter="OrderType eq 'SZ' and (OrderNbr eq 'SO00002786' or OrderNbr eq 'SO00005723' or OrderNbr eq 'SO00005728')"; Select='OrderType,OrderNbr,CustomerID,Status'; ExpRows=3; SumField=$null; ExpSum=$null }
)

$summary = @()
foreach ($t in $tests) {
    $short = ($t.Full -split '\.')[-1]
    # candidate entity-set names: from metadata (type ends with the full DAC name), then documented forms
    $cands = @()
    $cands += @($sets | Where-Object { $_.EntityType -match ('[._]' + [regex]::Escape($t.Full.Replace('.', '_')) + '$') -or $_.EntityType -match ([regex]::Escape($t.Full) + '$') } | ForEach-Object { $_.EntitySet })
    $cands += $short, $t.Full.Replace('.', '_')
    $cands = $cands | Where-Object { $_ } | Select-Object -Unique

    $result = $null
    foreach ($c in $cands) {
        $q = '?$filter=' + [Uri]::EscapeDataString($t.Filter) + '&$select=' + $t.Select + '&$top=20'
        $url = "$root/$c$q"
        try {
            $r = Invoke-RestMethod -Uri $url -Headers $headers -WebSession $session -UseBasicParsing
            $rows = @($r.value)
            $rows | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $outDir (($t.Obj -replace '[^A-Za-z0-9]', '') + '.json'))
            $sum = $null; if ($t.SumField) { $sum = ($rows | Measure-Object -Property $t.SumField -Sum).Sum }
            $ok = ($rows.Count -eq $t.ExpRows) -and (-not $t.SumField -or [decimal]$sum -eq [decimal]$t.ExpSum)
            $result = [pscustomobject]@{ Object=$t.Obj; EntitySetUsed=$c; Status='OK'; Rows=$rows.Count; ExpRows=$t.ExpRows; SumField=$t.SumField; Sum=$sum; ExpSum=$t.ExpSum; Check=$(if ($ok) {'PASS'} else {'CHECK'}); Error='' }
            break
        } catch {
            $result = [pscustomobject]@{ Object=$t.Obj; EntitySetUsed=$c; Status='FAILED'; Rows=''; ExpRows=$t.ExpRows; SumField=$t.SumField; Sum=''; ExpSum=$t.ExpSum; Check=''; Error=(Get-ErrorBody $_) }
        }
    }
    $summary += $result
    Write-Host ('{0,-26} {1,-8} {2} rows={3} {4} {5}' -f $t.Obj, $result.Status, $result.EntitySetUsed, $result.Rows, $result.Check, $result.Error)
}
$summary | Export-Csv -NoTypeInformation -Encoding UTF8 (Join-Path $outDir 'summary.csv')

# ---- 3. sign out and drop the auth header
try { Invoke-WebRequest -Uri "$BaseUrl/entity/auth/logout" -Method Post -WebSession $session -UseBasicParsing | Out-Null } catch {}
$headers = $null
Write-Host "`nDone. Results in $outDir"
