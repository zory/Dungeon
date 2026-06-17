<#
.SYNOPSIS
    Removes a git worktree after work is merged or abandoned.
.PARAMETER TaskId
    Task identifier (e.g., "TASK-001")
.PARAMETER Slug
    Short description slug (e.g., "add-health-feature")
.PARAMETER DeleteBranch
    Also delete the branch (default: false, only delete if merged)
.EXAMPLE
    .\remove-worktree.ps1 -TaskId "TASK-001" -Slug "add-health-feature"
#>
param(
    [Parameter(Mandatory)]
    [string]$TaskId,

    [Parameter(Mandatory)]
    [string]$Slug,

    [switch]$DeleteBranch
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$BranchName = "agent/$TaskId-$Slug"
$WorktreePath = Join-Path (Join-Path $RepoRoot "worktrees") "$TaskId-$Slug"

if (-not (Test-Path $WorktreePath)) {
    Write-Warning "Worktree not found at $WorktreePath"
} else {
    git -C $RepoRoot worktree remove $WorktreePath
    if ($LASTEXITCODE -eq 0) {
        Write-Output "Worktree removed: $WorktreePath"
    } else {
        Write-Error "Failed to remove worktree. It may have uncommitted changes."
        exit 1
    }
}

if ($DeleteBranch) {
    git -C $RepoRoot branch -d $BranchName
    if ($LASTEXITCODE -eq 0) {
        Write-Output "Branch deleted: $BranchName"
    } else {
        Write-Warning "Branch not deleted (may not be fully merged). Use -D to force."
    }
}
