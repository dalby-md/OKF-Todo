<#
.SYNOPSIS
    Copies the repository skills from .agents\skills to .claude\skills.

.DESCRIPTION
    .agents\skills is the source of truth. Codex reads it directly. Claude Code
    only reads .claude\skills, so that folder holds a committed copy.

    Run this script after every change under .agents\skills. With -Check, the
    script changes nothing and exits with code 1 if the copy is out of date.

.EXAMPLE
    .\tools\sync-claude-skills.ps1

.EXAMPLE
    .\tools\sync-claude-skills.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch] $Check
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repositoryRoot '.agents\skills'
$target = Join-Path $repositoryRoot '.claude\skills'

function Get-RelativeFiles([string] $root) {
    if (-not (Test-Path $root)) {
        return @()
    }

    $prefixLength = (Resolve-Path $root).Path.TrimEnd('\').Length + 1
    Get-ChildItem $root -Recurse -File | ForEach-Object { $_.FullName.Substring($prefixLength) }
}

function Get-Differences {
    $sourceFiles = @(Get-RelativeFiles $source)
    $targetFiles = @(Get-RelativeFiles $target)
    $differences = @()

    foreach ($file in $sourceFiles) {
        $targetFile = Join-Path $target $file
        if (-not (Test-Path $targetFile)) {
            $differences += "missing: $file"
        }
        elseif ((Get-FileHash (Join-Path $source $file)).Hash -ne (Get-FileHash $targetFile).Hash) {
            $differences += "changed: $file"
        }
    }

    foreach ($file in $targetFiles) {
        if ($sourceFiles -notcontains $file) {
            $differences += "extra:   $file"
        }
    }

    return $differences
}

if (-not (Test-Path $source)) {
    throw "Skill source folder not found: $source"
}

if ($Check) {
    $differences = @(Get-Differences)
    if ($differences.Count -eq 0) {
        Write-Output '.claude\skills matches .agents\skills.'
        exit 0
    }

    Write-Output '.claude\skills is out of date. Run .\tools\sync-claude-skills.ps1 to fix it.'
    $differences | ForEach-Object { Write-Output "  $_" }
    exit 1
}

if (Test-Path $target) {
    Remove-Item $target -Recurse -Force
}

New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
Copy-Item $source $target -Recurse

Write-Output "Copied $(@(Get-RelativeFiles $target).Count) files to .claude\skills."
