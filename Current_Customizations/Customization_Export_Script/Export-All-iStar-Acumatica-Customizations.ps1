<#
.SYNOPSIS
    Exports Acumatica customization projects listed in a CSV file.

.DESCRIPTION
    Reads Acumatica connection settings and credentials from a local .env file.

    Reads customization project names from CustomizationProjects.csv, logs in
    using Acumatica's session-based REST authentication, calls the
    /CustomizationApi/GetProject endpoint once per project, decodes each Base64
    package, and saves it as a ZIP file.

    While each customization is being generated and returned, the script displays
    an animated progress indicator. It also displays overall project progress.

.ENV FORMAT
    The .env file should contain:

        ACUMATICA_USERNAME=your_username
        ACUMATICA_PASSWORD=your_password
        ACUMATICA_BASE_URL=https://istar.privatecloudcorp.com/AcumaticaERP
        ACUMATICA_COMPANY=Company
        ACUMATICA_BRANCH=
        ACUMATICA_LOCALE=en-US

.CSV FORMAT
    The CSV must contain a column named:

        ProjectName

.NOTES
    - The Acumatica user must have the Customizer role.
    - OAuth is not used.
    - The .env file contains plain-text credentials.
    - Never commit the .env file to Git.
    - Duplicate and blank project names are removed automatically.
    - IsAutoResolveConflicts defaults to false.
    - The per-project progress indicator is indeterminate because Acumatica's
      GetProject endpoint does not report byte-level download progress.
#>

[CmdletBinding()]
param(
    [string]$EnvFilePath = "",

    [string]$ProjectCsvPath = "",

    [string]$OutputDirectory = "",

    [string]$BaseUrl = "",

    [string]$Company = "",

    [string]$Branch = "",

    [string]$Locale = "",

    [switch]$AutoResolveConflicts,

    [switch]$DoNotOpenExportFolder
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Resolve script directory
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
# Helper functions
# ---------------------------------------------------------------------------

function Resolve-PathRelativeToScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue,

        [Parameter(Mandatory = $true)]
        [string]$BaseDirectory
    )

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath(
        (Join-Path -Path $BaseDirectory -ChildPath $PathValue)
    )
}

function Import-DotEnvFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "The .env file was not found: $Path"
    }

    $values = @{}
    $lineNumber = 0

    foreach ($rawLine in Get-Content -LiteralPath $Path -ErrorAction Stop) {
        $lineNumber++

        $line = [string]$rawLine

        if ($null -eq $line) {
            continue
        }

        $line = $line.Trim()

        if (
            [string]::IsNullOrWhiteSpace($line) -or
            $line.StartsWith("#")
        ) {
            continue
        }

        if ($line.StartsWith("export ")) {
            $line = $line.Substring(7).Trim()
        }

        $separatorIndex = $line.IndexOf("=")

        if ($separatorIndex -lt 1) {
            throw "Invalid .env entry on line $lineNumber. Expected NAME=value."
        }

        $name = $line.Substring(0, $separatorIndex).Trim()
        $value = $line.Substring($separatorIndex + 1).Trim()

        if ($name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
            throw "Invalid variable name '$name' on .env line $lineNumber."
        }

        if (
            $value.Length -ge 2 -and
            (
                ($value.StartsWith('"') -and $value.EndsWith('"')) -or
                ($value.StartsWith("'") -and $value.EndsWith("'"))
            )
        ) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $value = $value.Replace('\n', "`n")
        $value = $value.Replace('\r', "`r")
        $value = $value.Replace('\t', "`t")

        $values[$name] = $value
    }

    return $values
}

function Get-RequiredEnvironmentValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$EnvironmentValues,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not $EnvironmentValues.ContainsKey($Name)) {
        throw "The required .env value '$Name' is missing."
    }

    $value = [string]$EnvironmentValues[$Name]

    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "The required .env value '$Name' is empty."
    }

    return $value
}

function Get-OptionalEnvironmentValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$EnvironmentValues,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string]$DefaultValue = ""
    )

    if (-not $EnvironmentValues.ContainsKey($Name)) {
        return $DefaultValue
    }

    return [string]$EnvironmentValues[$Name]
}

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
        [AllowEmptyCollection()]
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

    $headerNames = @($csvRows[0].PSObject.Properties.Name)

    if ($headerNames -notcontains "ProjectName") {
        throw "The CSV must contain a column named 'ProjectName'. Found columns: $($headerNames -join ', ')"
    }

    $projectNames = New-Object System.Collections.Generic.List[string]

    $seenNames = New-Object `
        "System.Collections.Generic.HashSet[string]" `
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

function Invoke-AcumaticaProjectDownload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [Microsoft.PowerShell.Commands.WebRequestSession]$WebSession,

        [Parameter(Mandatory = $true)]
        [string]$Body,

        [Parameter(Mandatory = $true)]
        [string]$ProjectName,

        [Parameter(Mandatory = $true)]
        [int]$CurrentProject,

        [Parameter(Mandatory = $true)]
        [int]$TotalProjects
    )

    $spinnerFrames = @("|", "/", "-", "\")
    $spinnerIndex = 0
    $startTime = Get-Date

    $powerShellInstance = [PowerShell]::Create()
    $asyncResult = $null

    try {
        $null = $powerShellInstance.AddScript({
            param(
                $RequestUri,
                $RequestSession,
                $RequestBody
            )

            Invoke-RestMethod `
                -Method Post `
                -Uri $RequestUri `
                -WebSession $RequestSession `
                -ContentType "application/json" `
                -Headers @{
                    Accept = "application/json"
                } `
                -Body $RequestBody `
                -ErrorAction Stop
        })

        $null = $powerShellInstance.AddArgument($Uri)
        $null = $powerShellInstance.AddArgument($WebSession)
        $null = $powerShellInstance.AddArgument($Body)

        $asyncResult = $powerShellInstance.BeginInvoke()

        while (-not $asyncResult.IsCompleted) {
            $elapsed = (Get-Date) - $startTime

            $overallPercent = [math]::Floor(
                (($CurrentProject - 1) / $TotalProjects) * 100
            )

            $spinner = $spinnerFrames[
                $spinnerIndex % $spinnerFrames.Count
            ]

            $elapsedText = $elapsed.ToString("mm\:ss")

            Write-Progress `
                -Id 1 `
                -Activity "Exporting Acumatica customizations" `
                -Status "Project $CurrentProject of $TotalProjects" `
                -PercentComplete $overallPercent

            Write-Progress `
                -Id 2 `
                -ParentId 1 `
                -Activity $ProjectName `
                -Status "$spinner Waiting for Acumatica - elapsed $elapsedText" `
                -PercentComplete -1

            $spinnerIndex++

            Start-Sleep -Milliseconds 200
        }

        $output = $powerShellInstance.EndInvoke($asyncResult)

        if ($powerShellInstance.HadErrors) {
            $errorMessages = @(
                $powerShellInstance.Streams.Error |
                    ForEach-Object {
                        $_.Exception.Message
                    }
            )

            if ($errorMessages.Count -gt 0) {
                throw ($errorMessages -join "; ")
            }

            throw "The Acumatica project request failed."
        }

        if ($null -eq $output -or $output.Count -eq 0) {
            throw "The Customization API returned an empty response."
        }

        if ($output.Count -eq 1) {
            return $output[0]
        }

        return $output
    }
    finally {
        Write-Progress `
            -Id 2 `
            -ParentId 1 `
            -Activity $ProjectName `
            -Completed

        if ($null -ne $powerShellInstance) {
            $powerShellInstance.Dispose()
        }
    }
}

# ---------------------------------------------------------------------------
# Resolve input file locations
# ---------------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($EnvFilePath)) {
    $EnvFilePath = Join-Path `
        -Path $scriptDirectory `
        -ChildPath ".env"
}
else {
    $EnvFilePath = Resolve-PathRelativeToScript `
        -PathValue $EnvFilePath `
        -BaseDirectory $scriptDirectory
}

if ([string]::IsNullOrWhiteSpace($ProjectCsvPath)) {
    $ProjectCsvPath = Join-Path `
        -Path $scriptDirectory `
        -ChildPath "CustomizationProjects.csv"
}
else {
    $ProjectCsvPath = Resolve-PathRelativeToScript `
        -PathValue $ProjectCsvPath `
        -BaseDirectory $scriptDirectory
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path `
        -Path $scriptDirectory `
        -ChildPath "Acumatica-Customization-Exports"
}
else {
    $OutputDirectory = Resolve-PathRelativeToScript `
        -PathValue $OutputDirectory `
        -BaseDirectory $scriptDirectory
}

$EnvFilePath = [System.IO.Path]::GetFullPath($EnvFilePath)
$ProjectCsvPath = [System.IO.Path]::GetFullPath($ProjectCsvPath)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

# ---------------------------------------------------------------------------
# Load .env settings
# ---------------------------------------------------------------------------

try {
    $envValues = Import-DotEnvFile -Path $EnvFilePath

    $username = Get-RequiredEnvironmentValue `
        -EnvironmentValues $envValues `
        -Name "ACUMATICA_USERNAME"

    $password = Get-RequiredEnvironmentValue `
        -EnvironmentValues $envValues `
        -Name "ACUMATICA_PASSWORD"

    if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
        $BaseUrl = Get-RequiredEnvironmentValue `
            -EnvironmentValues $envValues `
            -Name "ACUMATICA_BASE_URL"
    }

    if ([string]::IsNullOrWhiteSpace($Company)) {
        $Company = Get-RequiredEnvironmentValue `
            -EnvironmentValues $envValues `
            -Name "ACUMATICA_COMPANY"
    }

    if ([string]::IsNullOrWhiteSpace($Branch)) {
        $Branch = Get-OptionalEnvironmentValue `
            -EnvironmentValues $envValues `
            -Name "ACUMATICA_BRANCH" `
            -DefaultValue ""
    }

    if ([string]::IsNullOrWhiteSpace($Locale)) {
        $Locale = Get-OptionalEnvironmentValue `
            -EnvironmentValues $envValues `
            -Name "ACUMATICA_LOCALE" `
            -DefaultValue "en-US"
    }
}
catch {
    Write-Host ""
    Write-Error "Unable to load Acumatica settings from .env. $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Expected .env path:"
    Write-Host $EnvFilePath
    Write-Host ""
    exit 1
}

$BaseUrl = $BaseUrl.Trim().TrimEnd("/")
$Company = $Company.Trim()
$Branch = $Branch.Trim()
$Locale = $Locale.Trim()

$loginUrl = "$BaseUrl/entity/auth/login"
$logoutUrl = "$BaseUrl/entity/auth/logout"
$getProjectUrl = "$BaseUrl/CustomizationApi/GetProject"

# ---------------------------------------------------------------------------
# Load project names from CSV
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
# Create timestamped output directory
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
    Write-Warning "Could not save the normalized project list: $($_.Exception.Message)"
}

# ---------------------------------------------------------------------------
# Display settings
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "============================================================"
Write-Host " iStar Acumatica Customization Export"
Write-Host "============================================================"
Write-Host ""
Write-Host "Server:             $BaseUrl"
Write-Host "Company:            $Company"
Write-Host "Username:           $username"

if ([string]::IsNullOrWhiteSpace($Branch)) {
    Write-Host "Branch:             [default]"
}
else {
    Write-Host "Branch:             $Branch"
}

Write-Host "Environment file:   $EnvFilePath"
Write-Host "Project CSV:        $ProjectCsvPath"
Write-Host "CSV rows:           $($csvImportResult.CsvRowCount)"
Write-Host "Unique projects:    $($ProjectNames.Count)"
Write-Host "Duplicates removed: $($csvImportResult.DuplicateCount)"
Write-Host "Blank rows removed: $($csvImportResult.BlankRowCount)"
Write-Host "Output directory:   $runDirectory"
Write-Host "Resolve conflicts:  $([bool]$AutoResolveConflicts)"
Write-Host ""

# ---------------------------------------------------------------------------
# Authenticate and export
# ---------------------------------------------------------------------------

$session = $null
$loginSucceeded = $false
$fatalError = $null
$results = New-Object System.Collections.Generic.List[object]

try {
    $session = New-Object `
        Microsoft.PowerShell.Commands.WebRequestSession

    $loginBody = @{
        name     = $username
        password = $password
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

    $index = 0

    foreach ($projectName in $ProjectNames) {
        $index++

        $startingPercent = [math]::Floor(
            (($index - 1) / $ProjectNames.Count) * 100
        )

        Write-Progress `
            -Id 1 `
            -Activity "Exporting Acumatica customizations" `
            -Status "Starting project $index of $($ProjectNames.Count)" `
            -PercentComplete $startingPercent

        Write-Host "[$index/$($ProjectNames.Count)] Exporting: $projectName"

        try {
            $requestBody = @{
                projectName            = $projectName
                IsAutoResolveConflicts = [bool]$AutoResolveConflicts
            } | ConvertTo-Json

            $response = Invoke-AcumaticaProjectDownload `
                -Uri $getProjectUrl `
                -WebSession $session `
                -Body $requestBody `
                -ProjectName $projectName `
                -CurrentProject $index `
                -TotalProjects $ProjectNames.Count

            if ($response -is [System.Array] -and $response.Count -eq 1) {
                $response = $response[0]
            }

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

            $fileSizeMb = [math]::Round(
                ((Get-Item -LiteralPath $zipPath).Length / 1MB),
                2
            )

            Write-Host "      Saved: $zipPath"
            Write-Host "      Size:  $fileSizeMb MB"

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

        $completedPercent = [math]::Floor(
            ($index / $ProjectNames.Count) * 100
        )

        Write-Progress `
            -Id 1 `
            -Activity "Exporting Acumatica customizations" `
            -Status "Completed $index of $($ProjectNames.Count) projects" `
            -PercentComplete $completedPercent

        Write-Host ""
    }

    Write-Progress `
        -Id 1 `
        -Activity "Exporting Acumatica customizations" `
        -Completed
}
catch {
    $fatalError = $_.Exception.Message

    Write-Progress `
        -Id 1 `
        -Activity "Exporting Acumatica customizations" `
        -Completed

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
    Write-Progress `
        -Id 2 `
        -Activity "Current customization" `
        -Completed `
        -ErrorAction SilentlyContinue

    Write-Progress `
        -Id 1 `
        -Activity "Exporting Acumatica customizations" `
        -Completed `
        -ErrorAction SilentlyContinue

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

    $password = $null
    $username = $null
    $loginBody = $null
    $session = $null
}

# ---------------------------------------------------------------------------
# Write export report
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