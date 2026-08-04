<#
.SYNOPSIS
    Exports the configured Acumatica customization projects as individual ZIP files.

.DESCRIPTION
    Logs in using Acumatica's session-based REST authentication, calls the
    /CustomizationApi/GetProject endpoint once per project, decodes each Base64
    package, and saves it as a ZIP file.

    This script is designed to be run either:

    1. Directly from PowerShell.
    2. By double-clicking the accompanying BAT launcher.

.NOTES
    - The Acumatica user must have the Customizer role.
    - OAuth is not used.
    - The default Acumatica instance is:
      https://istar.privatecloudcorp.com/AcumaticaERP
    - The default company is:
      Company
    - IsAutoResolveConflicts defaults to false.
    - Review the $ProjectNames list before running.
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = "https://istar.privatecloudcorp.com/AcumaticaERP",

    [string]$Company = "Company",

    [string]$Branch = "",

    [string]$Locale = "en-US",

    [string]$OutputDirectory = "",

    [switch]$AutoResolveConflicts,

    [switch]$DoNotOpenExportFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Script location and output directory
# ---------------------------------------------------------------------------

# $PSScriptRoot always points to the folder containing this PS1 file.
# This prevents the output location from changing based on how the BAT file
# or PowerShell was launched.
$scriptDirectory = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path `
        -Path $scriptDirectory `
        -ChildPath "Acumatica-Customization-Exports"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    # Treat a relative output path as relative to the PS1 file, not the current
    # PowerShell working directory.
    $OutputDirectory = Join-Path `
        -Path $scriptDirectory `
        -ChildPath $OutputDirectory
}

# Convert the output directory into a full absolute path.
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

# ---------------------------------------------------------------------------
# Project names
# ---------------------------------------------------------------------------

# These names must exactly match the project names shown on the Acumatica
# Customization Projects screen.
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

# ---------------------------------------------------------------------------
# Helper functions
# ---------------------------------------------------------------------------

function Get-SafeFileName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $invalidChars = [System.IO.Path]::GetInvalidFileNameChars()
    $safeName = $Name

    foreach ($char in $invalidChars) {
        $safeName = $safeName.Replace([string]$char, "_")
    }

    return $safeName
}

function Write-ApiLog {
    [CmdletBinding()]
    param(
        $LogEntries
    )

    if ($null -eq $LogEntries) {
        return
    }

    foreach ($entry in $LogEntries) {
        $type = if ($null -ne $entry.logType -and $entry.logType) {
            [string]$entry.logType
        }
        else {
            "information"
        }

        $message = if ($null -ne $entry.message -and $entry.message) {
            [string]$entry.message
        }
        else {
            $entry | ConvertTo-Json -Compress -Depth 10
        }

        Write-Host "      [$type] $message"
    }
}

function Add-ExportResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.List[object]]$ResultList,

        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [string]$Status,

        [bool]$HasConflicts = $false,

        [string]$FilePath = "",

        [string]$ErrorMessage = ""
    )

    $ResultList.Add(
        [pscustomobject]@{
            ProjectName = $ProjectName
            Status      = $Status
            HasConflicts = $HasConflicts
            FilePath    = $FilePath
            Error       = $ErrorMessage
        }
    )
}

# ---------------------------------------------------------------------------
# Validate settings and create endpoint URLs
# ---------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    Write-Error "BaseUrl cannot be empty."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Company)) {
    Write-Error "Company cannot be empty."
    exit 1
}

if ($ProjectNames.Count -eq 0) {
    Write-Error "No Acumatica customization project names are configured."
    exit 1
}

$BaseUrl = $BaseUrl.Trim().TrimEnd("/")

$loginUrl = "$BaseUrl/entity/auth/login"
$logoutUrl = "$BaseUrl/entity/auth/logout"
$getProjectUrl = "$BaseUrl/CustomizationApi/GetProject"

# ---------------------------------------------------------------------------
# Create run directory
# ---------------------------------------------------------------------------

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"
$runDirectory = Join-Path -Path $OutputDirectory -ChildPath $timestamp
$reportPath = Join-Path -Path $runDirectory -ChildPath "Export-Report.csv"

try {
    New-Item `
        -ItemType Directory `
        -Path $runDirectory `
        -Force `
        -ErrorAction Stop | Out-Null
}
catch {
    Write-Host ""
    Write-Error "Unable to create the export directory '$runDirectory'. $($_.Exception.Message)"
    exit 1
}

# ---------------------------------------------------------------------------
# Authentication preparation
# ---------------------------------------------------------------------------

$credential = $null
$plainPassword = $null
$session = $null
$loginSucceeded = $false
$results = New-Object System.Collections.Generic.List[object]
$fatalError = $null

Write-Host ""
Write-Host "============================================================"
Write-Host " iStar Acumatica Customization Export"
Write-Host "============================================================"
Write-Host ""
Write-Host "Server:             $BaseUrl"
Write-Host "Company:            $Company"
Write-Host "Branch:             $(if ([string]::IsNullOrWhiteSpace($Branch)) { '[default]' } else { $Branch })"
Write-Host "Projects:           $($ProjectNames.Count)"
Write-Host "Output directory:   $runDirectory"
Write-Host "Resolve conflicts:  $([bool]$AutoResolveConflicts)"
Write-Host ""

try {
    $credential = Get-Credential `
        -Message "Enter your Acumatica username and password"

    if ($null -eq $credential) {
        throw "No Acumatica credentials were provided."
    }

    if ([string]::IsNullOrWhiteSpace($credential.UserName)) {
        throw "The Acumatica username cannot be empty."
    }

    $plainPassword = $credential.GetNetworkCredential().Password

    if ([string]::IsNullOrWhiteSpace($plainPassword)) {
        throw "The Acumatica password cannot be empty."
    }

    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

    $loginBody = @{
        name     = $credential.UserName
        password = $plainPassword
        company  = $Company
        branch   = $Branch
        locale   = $Locale
    } | ConvertTo-Json

    Write-Host "Signing in to Acumatica..."

    Invoke-RestMethod `
        -Method Post `
        -Uri $loginUrl `
        -WebSession $session `
        -ContentType "application/json" `
        -Headers @{
            Accept = "application/json"
        } `
        -Body $loginBody `
        -ErrorAction Stop | Out-Null

    $loginSucceeded = $true

    Write-Host "Signed in successfully."
    Write-Host ""

    # -----------------------------------------------------------------------
    # Export each project
    # -----------------------------------------------------------------------

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
                -Headers @{
                    Accept = "application/json"
                } `
                -Body $requestBody `
                -ErrorAction Stop

            if ($null -eq $response) {
                throw "The Customization API returned an empty response."
            }

            Write-ApiLog -LogEntries $response.log

            $projectContentBase64 = [string]$response.projectContentBase64

            if ([string]::IsNullOrWhiteSpace($projectContentBase64)) {
                throw "The API returned no projectContentBase64 value."
            }

            try {
                $zipBytes = [Convert]::FromBase64String(
                    $projectContentBase64
                )
            }
            catch {
                throw "The API response could not be decoded from Base64. $($_.Exception.Message)"
            }

            # Standard ZIP files begin with the bytes "PK".
            if (
                $null -eq $zipBytes -or
                $zipBytes.Length -lt 2 -or
                $zipBytes[0] -ne 0x50 -or
                $zipBytes[1] -ne 0x4B
            ) {
                throw "The decoded response does not have a valid ZIP signature."
            }

            $safeName = Get-SafeFileName -Name $projectName
            $zipPath = Join-Path `
                -Path $runDirectory `
                -ChildPath "$safeName.zip"

            [System.IO.File]::WriteAllBytes($zipPath, $zipBytes)

            if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
                throw "The ZIP file was not found after it was written."
            }

            $hasConflicts = [bool]$response.hasConflicts

            $status = if ($hasConflicts) {
                "ExportedWithConflicts"
            }
            else {
                "Exported"
            }

            Add-ExportResult `
                -ResultList $results `
                -ProjectName $projectName `
                -Status $status `
                -HasConflicts $hasConflicts `
                -FilePath $zipPath

            Write-Host "      Saved: $zipPath"

            if ($hasConflicts) {
                Write-Warning "The API reported file-system conflicts for this project."
            }
        }
        catch {
            $message = $_.Exception.Message

            Write-Warning "Failed to export '$projectName': $message"

            Add-ExportResult `
                -ResultList $results `
                -ProjectName $projectName `
                -Status "Failed" `
                -HasConflicts $false `
                -FilePath "" `
                -ErrorMessage $message
        }

        Write-Host ""
    }
}
catch {
    $fatalError = $_.Exception.Message

    Write-Host ""
    Write-Error "The customization export could not continue. $fatalError"

    # When login or setup fails before projects are processed, create report
    # rows for projects that were not attempted.
    $processedProjectNames = @(
        $results |
            ForEach-Object {
                $_.ProjectName
            }
    )

    foreach ($projectName in $ProjectNames) {
        if ($projectName -notin $processedProjectNames) {
            Add-ExportResult `
                -ResultList $results `
                -ProjectName $projectName `
                -Status "NotAttempted" `
                -HasConflicts $false `
                -FilePath "" `
                -ErrorMessage $fatalError
        }
    }
}
finally {
    if ($loginSucceeded -and $null -ne $session) {
        try {
            Write-Host ""
            Write-Host "Signing out..."

            Invoke-RestMethod `
                -Method Post `
                -Uri $logoutUrl `
                -WebSession $session `
                -ContentType "application/json" `
                -Headers @{
                    Accept = "application/json"
                } `
                -ErrorAction Stop | Out-Null

            Write-Host "Signed out."
        }
        catch {
            Write-Warning "The export finished, but logout returned an error: $($_.Exception.Message)"
        }
    }

    # Remove the plain-text password reference as soon as it is no longer
    # needed. This does not place the password in the report or on disk.
    $plainPassword = $null
    $credential = $null
    $session = $null
}

# ---------------------------------------------------------------------------
# Write report
# ---------------------------------------------------------------------------

try {
    $results |
        Export-Csv `
            -Path $reportPath `
            -NoTypeInformation `
            -Encoding UTF8 `
            -Force `
            -ErrorAction Stop
}
catch {
    Write-Host ""
    Write-Error "Unable to write the export report. $($_.Exception.Message)"
    exit 1
}

$successCount = @(
    $results |
        Where-Object {
            $_.Status -eq "Exported" -or
            $_.Status -eq "ExportedWithConflicts"
        }
).Count

$conflictCount = @(
    $results |
        Where-Object {
            $_.Status -eq "ExportedWithConflicts"
        }
).Count

$failedCount = @(
    $results |
        Where-Object {
            $_.Status -eq "Failed"
        }
).Count

$notAttemptedCount = @(
    $results |
        Where-Object {
            $_.Status -eq "NotAttempted"
        }
).Count

Write-Host ""
Write-Host "============================================================"
Write-Host " Export Summary"
Write-Host "============================================================"
Write-Host ""
Write-Host "Successful:          $successCount"
Write-Host "With conflicts:      $conflictCount"
Write-Host "Failed:              $failedCount"
Write-Host "Not attempted:       $notAttemptedCount"
Write-Host ""
Write-Host "Export directory:"
Write-Host $runDirectory
Write-Host ""
Write-Host "Export report:"
Write-Host $reportPath
Write-Host ""

if (-not $DoNotOpenExportFolder) {
    try {
        Start-Process `
            -FilePath "explorer.exe" `
            -ArgumentList "`"$runDirectory`"" `
            -ErrorAction Stop
    }
    catch {
        Write-Warning "The export folder could not be opened automatically: $($_.Exception.Message)"
    }
}

if (
    $null -ne $fatalError -or
    $failedCount -gt 0 -or
    $notAttemptedCount -gt 0
) {
    Write-Host "The export completed with one or more errors."
    Write-Host "Review Export-Report.csv for the exact details."
    Write-Host ""

    exit 1
}

Write-Host "All configured projects were exported successfully."

if ($conflictCount -gt 0) {
    Write-Host "$conflictCount project(s) were exported with reported file-system conflicts."
}

Write-Host ""

exit 0