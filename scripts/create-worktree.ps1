<#
.SYNOPSIS
    Creates a git worktree for an agent task branch.
.PARAMETER TaskId
    Task identifier (e.g., "TASK-001")
.PARAMETER Slug
    Short description slug (e.g., "add-health-feature")
.EXAMPLE
    .\create-worktree.ps1 -TaskId "TASK-001" -Slug "add-health-feature"
#>
param(
    [Parameter(Mandatory)]
    [string]$TaskId,

    [Parameter(Mandatory)]
    [string]$Slug
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BranchName = "agent/$TaskId-$Slug"
$WorktreePath = Join-Path (Join-Path $RepoRoot "worktrees") "$TaskId-$Slug"

if (Test-Path $WorktreePath) {
    Write-Error "Worktree already exists at $WorktreePath"
    exit 1
}

# Ensure main working tree is clean
$Status = git -C $RepoRoot status --porcelain
if ($Status) {
    Write-Warning "Working tree has uncommitted changes:"
    Write-Output $Status
    Write-Error "Clean the working tree before creating a worktree."
    exit 1
}

# Create branch and worktree
git -C $RepoRoot branch $BranchName 2>$null
git -C $RepoRoot worktree add $WorktreePath $BranchName

if ($LASTEXITCODE -eq 0) {
    Write-Output "Worktree created:"
    Write-Output "  Branch: $BranchName"
    Write-Output "  Path:   $WorktreePath"
} else {
    Write-Error "Failed to create worktree."
    exit 1
}
