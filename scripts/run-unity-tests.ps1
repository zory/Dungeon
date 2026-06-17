<#
.SYNOPSIS
    Runs Unity EditMode or PlayMode tests in batch mode.
.PARAMETER TestMode
    "editmode" or "playmode" (default: editmode)
.PARAMETER Filter
    Optional test filter (e.g., "Dungeon.Tests.EditMode.GridTests")
.EXAMPLE
    .\run-unity-tests.ps1
    .\run-unity-tests.ps1 -TestMode playmode
    .\run-unity-tests.ps1 -TestMode editmode -Filter "GridTests"
#>
param(
    [ValidateSet("editmode", "playmode")]
    [string]$TestMode = "editmode",

    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "Dungeon"
$ResultsDir = Join-Path (Join-Path $RepoRoot "artifacts") $TestMode
$ResultsFile = Join-Path $ResultsDir "results.xml"
$LogFile = Join-Path $ResultsDir "unity.log"

# Read Unity version from ProjectVersion.txt
$VersionFile = Join-Path (Join-Path $ProjectPath "ProjectSettings") "ProjectVersion.txt"
$VersionLine = Get-Content $VersionFile | Select-String "m_EditorVersion:"
$UnityVersion = ($VersionLine -replace "m_EditorVersion:\s*", "").Trim()

# Find Unity Editor
$HubPath = "C:\Program Files\Unity\Hub\Editor"
$UnityEditor = Join-Path (Join-Path (Join-Path $HubPath $UnityVersion) "Editor") "Unity.exe"

if (-not (Test-Path $UnityEditor)) {
    # Try to find any matching major version
    $MajorVersion = $UnityVersion.Split('.')[0]
    $Candidates = Get-ChildItem $HubPath -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name.StartsWith($MajorVersion) } |
        Sort-Object Name -Descending
    if ($Candidates) {
        $UnityEditor = Join-Path (Join-Path $Candidates[0].FullName "Editor") "Unity.exe"
        Write-Warning "Exact version $UnityVersion not found. Using $($Candidates[0].Name)"
    } else {
        Write-Error "Unity Editor not found at $HubPath for version $UnityVersion"
        exit 1
    }
}

Write-Output "Unity: $UnityEditor"
Write-Output "Project: $ProjectPath"
Write-Output "Test mode: $TestMode"

# Create results directory
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

# Build arguments
$Args = @(
    "-runTests",
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-testResults", $ResultsFile,
    "-logFile", $LogFile,
    "-testPlatform", $TestMode
)

if ($Filter) {
    $Args += "-testFilter"
    $Args += $Filter
    Write-Output "Filter: $Filter"
}

Write-Output "Running tests..."
& $UnityEditor @Args

$ExitCode = $LASTEXITCODE

if (Test-Path $ResultsFile) {
    Write-Output ""
    Write-Output "Results: $ResultsFile"

    # Parse basic results from XML
    [xml]$Results = Get-Content $ResultsFile
    $TestRun = $Results.'test-run'
    if ($TestRun) {
        Write-Output "Total: $($TestRun.total) | Passed: $($TestRun.passed) | Failed: $($TestRun.failed) | Skipped: $($TestRun.skipped)"
    }
} else {
    Write-Warning "No results file generated. Check log: $LogFile"
}

Write-Output "Log: $LogFile"
exit $ExitCode
