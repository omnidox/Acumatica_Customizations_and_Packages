<#
.SYNOPSIS
    Exports Acumatica customization projects listed in a CSV file.

.DESCRIPTION
    Reads customization project names from CustomizationProjects.csv, logs in
    using Acumatica's session-based REST authentication, calls the
    /CustomizationApi/GetProject endpoint once per project, decodes each
    Base64 package, and saves it as a ZIP file.

    This script can be run:

    1. Directly from PowerShell.
    2. By double-clicking Run-Customization-Export.bat.

.CSV FORMAT
    The CSV must contain a column named:

        ProjectName

    Example:

        ProjectName
        "ConsignmentOrdersBE"
        "ASCJewelryLibrary[v1.2.2]"

.NOTES
    - The Acumatica user must have the Customizer role.
    - OAuth is not used.
    - Default Acumatica instance:
      https://istar.privatecloudcorp.com/AcumaticaERP
    - Default company:
      Company
    - Duplicate and blank project names are removed automatically.
    - IsAutoResolveConflicts defaults to false.
#>

[CmdletBinding()]
param(
    [string]$BaseUrl = "https://istar.privatecloudcorp.com/AcumaticaERP",

    [string]$Company = "Company",

    [string]$Branch = "",

    [string]$Locale = "en-US",

    [string]$ProjectCsvPath = "",

    [string]$OutputDirectory = "",

    [switch]$AutoResolveConflicts,

    [switch]$DoNotOpenExportFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Resolve the script directory
# ---------------------------------------------------------------------------

$scriptDirectory = $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
}

if ([string]::IsNullOrWhiteSpace($scriptDirectory)) {
    $scriptDirectory = (Get-Location).Path
}

$scriptDirectory = [System.IO.Path]::GetFullPath($scriptDirectory)

# ---------------------------------------------------------------------------
# Resolve the CSV path
# ---------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($ProjectCsvPath)) {
    $ProjectCsvPath = Join-Path `
        -Path $scriptDirectory `
        -ChildPath "CustomizationProjects.csv"
}
elseif (-not [System.IO.Path]::IsPathRooted($ProjectCsvPath)) {
    $ProjectCsvPath = Join-Path `
        -Path $scriptDirectory `
        -ChildPath $ProjectCsvPath
}

$ProjectCsvPath = [System.IO.Path]::GetFullPath($ProjectCsvPath)

# ---------------------------------------------------------------------------
# Resolve the output directory
# ---------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path `
        -Path $scriptDirectory `
        -ChildPath "Acumatica-Customization-Exports"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path `
        -Path $scriptDirectory `
        -ChildPath $OutputDirectory
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

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

    foreach ($entry in @($LogEntries)) {
        $type = "information"
        $message = ""

        if (
            $null -ne $entry.PSObject.Properties["logType"] -and
            -not [string]::IsNullOrWhiteSpace([string]$entry.logType)
        ) {
            $type = [string]$entry.logType
        }

        if (
            $null -ne $entry.PSObject.Properties["message"] -and
            -not [string]::IsNullOrWhiteSpace([string]$entry.message)
        ) {
            $message = [string]$entry.message
        }
        else {
            $message = $entry | ConvertTo-Json -Compress -Depth 10
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
            ProjectName  = $ProjectName
            Status       = $Status
            HasConflicts = $HasConflicts
            FilePath     = $FilePath
            Error        = $ErrorMessage
        }
    )
}

function Get-ProjectNamesFromCsv {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsvPath
    )

    if (-not (Test-Path -LiteralPath $CsvPath -PathType Leaf)) {
        throw "The project CSV file was not found: $CsvPath"
    }

    try {
        $csvRows = @(
            Import-Csv `
                -LiteralPath $CsvPath `
                -ErrorAction Stop
        )
    }
    catch {
        throw "The CSV file could not be read. $($_.Exception.Message)"
    }

    if ($csvRows.Count -eq 0) {
        throw "The CSV file contains no project rows."
    }

    $headerNames = @(
        $csvRows[0].PSObject.Properties.Name
    )

    if ($headerNames -notcontains "ProjectName") {
        throw "The CSV must contain a column named 'ProjectName'. Found columns: $($headerNames -join ', ')"
    }

    $projectNames = New-Object System.Collections.Generic.List[string]
    $seenNames = New-Object "System.Collections.Generic.HashSet[string]" `
        ([System.StringComparer]::OrdinalIgnoreCase)

    $blankRowCount = 0
    $duplicateCount = 0

    foreach ($row in $csvRows) {
        $projectName = [string]$row.ProjectName

        if ($null -ne $projectName) {
            $projectName = $projectName.Trim()
        }

        if ([string]::IsNullOrWhiteSpace($projectName)) {
            $blankRowCount++
            continue
        }

        if ($seenNames.Add($projectName)) {
            $projectNames.Add($projectName)
        }
        else {
            $duplicateCount++
            Write-Warning "Duplicate project name ignored: $projectName"
        }
    }

    if ($projectNames.Count -eq 0) {
        throw "The CSV contains no valid project names."
    }

    return [pscustomobject]@{
        ProjectNames   = $projectNames.ToArray()
        BlankRowCount  = $blankRowCount
        DuplicateCount = $duplicateCount
        CsvRowCount    = $csvRows.Count
    }
}

# ---------------------------------------------------------------------------
# Validate connection settings
# ---------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    Write-Error "BaseUrl cannot be empty."
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Company)) {
    Write-Error "Company cannot be empty."
    exit 1
}

$BaseUrl = $BaseUrl.Trim().TrimEnd("/")

$loginUrl = "$BaseUrl/entity/auth/login"
$logoutUrl = "$BaseUrl/entity/auth/logout"
$getProjectUrl = "$BaseUrl/CustomizationApi/GetProject"

# ---------------------------------------------------------------------------
# Load project names from the CSV
# ---------------------------------------------------------------------------

try {
    $csvImportResult = Get-ProjectNamesFromCsv `
        -CsvPath $ProjectCsvPath

    $ProjectNames = @($csvImportResult.ProjectNames)
}
catch {
    Write-Host ""
    Write-Error "Unable to load customization project names. $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Expected CSV path:"
    Write-Host $ProjectCsvPath
    Write-Host ""
    exit 1
}

# ---------------------------------------------------------------------------
# Create the timestamped run directory
# ---------------------------------------------------------------------------

$timestamp = Get-Date -Format "yyyy-MM-dd_HHmmss"

$runDirectory = Join-Path `
    -Path $OutputDirectory `
    -ChildPath $timestamp

$reportPath = Join-Path `
    -Path $runDirectory `
    -ChildPath "Export-Report.csv"

$projectListCopyPath = Join-Path `
    -Path $runDirectory `
    -ChildPath "CustomizationProjects-Used.csv"

try {
    New-Item `
        -ItemType Directory `
        -Path $runDirectory `
        -Force `
        -ErrorAction Stop |
        Out-Null
}
catch {
    Write-Host ""
    Write-Error "Unable to create the export directory '$runDirectory'. $($_.Exception.Message)"
    exit 1
}

# Save a normalized copy of the exact project list used for this export.
try {
    $ProjectNames |
        ForEach-Object {
            [pscustomobject]@{
                ProjectName = $_
            }
        } |
        Export-Csv `
            -LiteralPath $projectListCopyPath `
            -NoTypeInformation `
            -Encoding UTF8 `
            -Force `
            -ErrorAction Stop
}
catch {
    Write-Warning "Could not save the normalized project-list copy: $($_.Exception.Message)"
}

# ---------------------------------------------------------------------------
# Authentication and result variables
# ---------------------------------------------------------------------------

$credential = $null
$plainPassword = $null
$session = $null
$loginSucceeded = $false
$fatalError = $null

$results = New-Object System.Collections.Generic.List[object]

Write-Host ""
Write-Host "============================================================"
Write-Host " iStar Acumatica Customization Export"
Write-Host "============================================================"
Write-Host ""
Write-Host "Server:             $BaseUrl"
Write-Host "Company:            $Company"

if ([string]::IsNullOrWhiteSpace($Branch)) {
    Write-Host "Branch:             [default]"
}
else {
    Write-Host "Branch:             $Branch"
}

Write-Host "Project CSV:        $ProjectCsvPath"
Write-Host "CSV rows:           $($csvImportResult.CsvRowCount)"
Write-Host "Unique projects:    $($ProjectNames.Count)"
Write-Host "Duplicates removed: $($csvImportResult.DuplicateCount)"
Write-Host "Blank rows removed: $($csvImportResult.BlankRowCount)"
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

    $session = New-Object `
        Microsoft.PowerShell.Commands.WebRequestSession

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
        -ErrorAction Stop |
        Out-Null

    $loginSucceeded = $true

    Write-Host "Signed in successfully."
    Write-Host ""

    # -----------------------------------------------------------------------
    # Export each project from the CSV
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

            if ($null -ne $response.PSObject.Properties["log"]) {
                Write-ApiLog -LogEntries $response.log
            }

            $projectContentBase64 = ""

            if (
                $null -ne
                $response.PSObject.Properties["projectContentBase64"]
            ) {
                $projectContentBase64 =
                    [string]$response.projectContentBase64
            }

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

            # Standard ZIP files begin with the ASCII bytes PK.
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

            [System.IO.File]::WriteAllBytes(
                $zipPath,
                $zipBytes
            )

            if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
                throw "The ZIP file was not found after it was written."
            }

            $hasConflicts = $false

            if (
                $null -ne
                $response.PSObject.Properties["hasConflicts"]
            ) {
                $hasConflicts = [bool]$response.hasConflicts
            }

            if ($hasConflicts) {
                $status = "ExportedWithConflicts"
            }
            else {
                $status = "Exported"
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
                -ErrorAction Stop |
                Out-Null

            Write-Host "Signed out."
        }
        catch {
            Write-Warning "The export finished, but logout returned an error: $($_.Exception.Message)"
        }
    }

    $plainPassword = $null
    $credential = $null
    $session = $null
}

# ---------------------------------------------------------------------------
# Write the export report
# ---------------------------------------------------------------------------

try {
    $results |
        Export-Csv `
            -LiteralPath $reportPath `
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

# ---------------------------------------------------------------------------
# Calculate summary counts
# ---------------------------------------------------------------------------

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
Write-Host "CSV rows:            $($csvImportResult.CsvRowCount)"
Write-Host "Unique projects:     $($ProjectNames.Count)"
Write-Host "Duplicates removed:  $($csvImportResult.DuplicateCount)"
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
Write-Host "Project list used:"
Write-Host $projectListCopyPath
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
    Write-Host "Review Export-Report.csv for exact details."
    Write-Host ""

    exit 1
}

Write-Host "All projects listed in the CSV were exported successfully."

if ($conflictCount -gt 0) {
    Write-Host "$conflictCount project(s) were exported with reported file-system conflicts."
}

Write-Host ""

exit 0