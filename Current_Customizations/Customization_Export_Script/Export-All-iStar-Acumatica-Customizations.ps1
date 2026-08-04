<#
.SYNOPSIS
    Exports the Acumatica customization projects listed below as individual ZIP files.

.DESCRIPTION
    Logs in with Acumatica's session-based REST authentication, calls the supported
    /CustomizationApi/GetProject endpoint once per project, decodes each Base64
    package, and saves it as a ZIP.

.NOTES
    - The Acumatica user must have the Customizer role.
    - OAuth is not used.
    - By default, IsAutoResolveConflicts is false so the database project is exported
      without automatically pulling physical IIS file changes into it.
    - Review the $ProjectNames list before running.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [string]$Company,

    [string]$Branch = "",

    [string]$Locale = "en-US",

    [string]$OutputDirectory = ".\Acumatica-Customization-Exports",

    [switch]$AutoResolveConflicts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Projects transcribed from the supplied Customization Projects screenshot.
# Correct any spelling here if a project name differs in Acumatica.
$ProjectNames = @(
    "ConsignmentOrdersBE",
    "ASCJewelryLibrary[v1.2.2]",
    "TRUECOMMERCE[25.193.0171][9.0.1.137]",
    "ASCIStarWMSCustomization[June19]",
    "AsgardLabels[Basic][25.201.0213][6.4.2.2]",
    "AsgardLabels[RomanSunStone][25.200.0248][1.0.0]",
    "OneUCCPerPackage",
    "OneLabelPerPackage",
    "UserRoleExtender",
    "AsgardButtonControl[06.09.2026]",
    "iStarCustomizations[25.201][July1]",
    "MasterPackISV[25.201][06.25.2026]",
    "MasterPackExtension[07.10.2026][1]",
    "CustomWMSManualPackTransfer[07.13.2026][1]",
    "POReceiptLineAdditionalColumn[06.19.2026][1]",
    "Velixo[25R2]",
    "SplitGIsAndReports[24.209.0013][April426]",
    "iStarShippingRestrictionsCustomizations[06.30.2026]",
    "MonthlyForecastReferenceTable[06.30.2026][1]",
    "FlexManufacturing25R201v260422"
)

function Get-SafeFileName {
    param([Parameter(Mandatory = $true)][string]$Name)

    $invalidChars = [System.IO.Path]::GetInvalidFileNameChars()
    $safeName = $Name

    foreach ($char in $invalidChars) {
        $safeName = $safeName.Replace([string]$char, "_")
    }

    return $safeName
}

function Write-ApiLog {
    param($LogEntries)

    if ($null -eq $LogEntries) {
        return
    }

    foreach ($entry in $LogEntries) {
        $type = if ($entry.logType) { $entry.logType } else { "information" }
        $message = if ($entry.message) { $entry.message } else { $entry | ConvertTo-Json -Compress }
        Write-Host "      [$type] $message"
    }
}

$BaseUrl = $BaseUrl.TrimEnd("/")
$loginUrl = "$BaseUrl/entity/auth/login"
$logoutUrl = "$BaseUrl/entity/auth/logout"
$getProjectUrl = "$BaseUrl/CustomizationApi/GetProject"

$credential = Get-Credential -Message "Enter the Acumatica username and password"

$plainPassword = $credential.GetNetworkCredential().Password
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$loginBody = @{
    name     = $credential.UserName
    password = $plainPassword
    company  = $Company
    branch   = $Branch
    locale   = $Locale
} | ConvertTo-Json

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$runDirectory = Join-Path $OutputDirectory $timestamp
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$results = New-Object System.Collections.Generic.List[object]

try {
    Write-Host ""
    Write-Host "Signing in to $BaseUrl ..."

    Invoke-RestMethod `
        -Method Post `
        -Uri $loginUrl `
        -WebSession $session `
        -ContentType "application/json" `
        -Body $loginBody | Out-Null

    Write-Host "Signed in successfully."
    Write-Host "Export directory: $runDirectory"
    Write-Host ""

    $index = 0

    foreach ($projectName in $ProjectNames) {
        $index++
        Write-Host "[$index/$($ProjectNames.Count)] Exporting: $projectName"

        try {
            $requestBody = @{
                projectName           = $projectName
                IsAutoResolveConflicts = [bool]$AutoResolveConflicts
            } | ConvertTo-Json

            $response = Invoke-RestMethod `
                -Method Post `
                -Uri $getProjectUrl `
                -WebSession $session `
                -ContentType "application/json" `
                -Headers @{ Accept = "application/json" } `
                -Body $requestBody

            Write-ApiLog -LogEntries $response.log

            if ([string]::IsNullOrWhiteSpace($response.projectContentBase64)) {
                throw "The API returned no projectContentBase64 value."
            }

            $zipBytes = [Convert]::FromBase64String($response.projectContentBase64)

            # ZIP files normally start with the bytes PK.
            if ($zipBytes.Length -lt 2 -or $zipBytes[0] -ne 0x50 -or $zipBytes[1] -ne 0x4B) {
                throw "The decoded response does not have a valid ZIP signature."
            }

            $safeName = Get-SafeFileName -Name $projectName
            $zipPath = Join-Path $runDirectory "$safeName.zip"
            [System.IO.File]::WriteAllBytes($zipPath, $zipBytes)

            $status = if ($response.hasConflicts) { "ExportedWithConflicts" } else { "Exported" }

            $results.Add([pscustomobject]@{
                ProjectName = $projectName
                Status      = $status
                HasConflicts = [bool]$response.hasConflicts
                FilePath    = $zipPath
                Error       = ""
            })

            Write-Host "      Saved: $zipPath"
            if ($response.hasConflicts) {
                Write-Warning "The API reported file-system conflicts for this project."
            }
        }
        catch {
            $message = $_.Exception.Message
            Write-Warning "Failed to export '$projectName': $message"

            $results.Add([pscustomobject]@{
                ProjectName = $projectName
                Status      = "Failed"
                HasConflicts = $false
                FilePath    = ""
                Error       = $message
            })
        }

        Write-Host ""
    }
}
finally {
    try {
        Write-Host "Signing out..."
        Invoke-RestMethod `
            -Method Post `
            -Uri $logoutUrl `
            -WebSession $session `
            -ContentType "application/json" | Out-Null
        Write-Host "Signed out."
    }
    catch {
        Write-Warning "The export finished, but logout returned an error: $($_.Exception.Message)"
    }

    $plainPassword = $null
    $credential = $null
}

$reportPath = Join-Path $runDirectory "Export-Report.csv"
$results | Export-Csv -Path $reportPath -NoTypeInformation -Encoding UTF8

$successCount = @($results | Where-Object { $_.Status -ne "Failed" }).Count
$failedCount = @($results | Where-Object { $_.Status -eq "Failed" }).Count

Write-Host ""
Write-Host "Export complete."
Write-Host "Successful: $successCount"
Write-Host "Failed:     $failedCount"
Write-Host "Report:     $reportPath"

if ($failedCount -gt 0) {
    Write-Host ""
    Write-Host "Review Export-Report.csv. The most likely cause is a project-name spelling"
    Write-Host "difference between the screenshot and the exact name stored in Acumatica."
    exit 1
}
