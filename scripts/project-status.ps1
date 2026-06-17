<#
.SYNOPSIS
    Shows a summary of the project status: tasks, git state, and recent activity.
.EXAMPLE
    .\project-status.ps1
#>

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Write-Output "=== Dungeon Project Status ==="
Write-Output ""

# Git status
Write-Output "--- Git ---"
$Branch = git -C $RepoRoot branch --show-current
Write-Output "Branch: $Branch"
$Status = git -C $RepoRoot status --short
if ($Status) {
    Write-Output "Uncommitted changes:"
    Write-Output $Status
} else {
    Write-Output "Working tree clean"
}
Write-Output ""

# Worktrees
Write-Output "--- Worktrees ---"
$Worktrees = git -C $RepoRoot worktree list
Write-Output $Worktrees
Write-Output ""

# Tasks
Write-Output "--- Tasks ---"
$TaskDirs = @("backlog", "active", "review", "completed")
foreach ($Dir in $TaskDirs) {
    $Path = Join-Path (Join-Path $RepoRoot "tasks") $Dir
    $Count = (Get-ChildItem $Path -Filter "*.md" -ErrorAction SilentlyContinue).Count
    Write-Output "${Dir}: $Count"
}
Write-Output ""

# Human tasks
Write-Output "--- Owner Todo ---"
$TodoPath = Join-Path $RepoRoot "todo"
$TodoFiles = Get-ChildItem $TodoPath -Filter "*.md" -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -notlike "*\done*" }
if ($TodoFiles) {
    foreach ($File in $TodoFiles) {
        Write-Output "  - $($File.BaseName)"
    }
} else {
    Write-Output "  (none)"
}
Write-Output ""

# Recent commits
Write-Output "--- Recent Commits ---"
git -C $RepoRoot log --oneline -5
