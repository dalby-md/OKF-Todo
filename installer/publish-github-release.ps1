[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(\.\d+)?$')]
    [string]$Version = '0.2.0',

    [string]$Tag,

    [string]$Title,

    [string]$NotesFile,

    [switch]$Draft,

    [switch]$PublishDraft,

    [switch]$Latest
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$tag = if ([string]::IsNullOrWhiteSpace($Tag)) { "v$Version" } else { $Tag.Trim() }
$releaseTitle = if ([string]::IsNullOrWhiteSpace($Title)) { "OKF-Todo $Version" } else { $Title.Trim() }
$installerPath = Join-Path `
    $repoRoot `
    "artifacts\installer\Okf-Todo-$Version-win-x64-setup.exe"
$checksumPath = "$installerPath.sha256"

if ($Draft -and $PublishDraft) {
    throw 'Draft and PublishDraft cannot be used together.'
}

if ($Draft -and $Latest) {
    throw 'A draft cannot be marked Latest until it is published. Use Latest with PublishDraft.'
}

if ([string]::IsNullOrWhiteSpace($tag)) {
    throw 'A release tag is required.'
}

if ($tag -notmatch '^v\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Release tag '$tag' must use the form v1.2.3 or v1.2.3-suffix."
}

if ($null -eq (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) was not found. Install it and authenticate with gh auth login.'
}

Push-Location $repoRoot
try {
    if ($PublishDraft) {
        $editArguments = @('release', 'edit', $tag, '--draft=false')
        if ($Latest) {
            $editArguments += '--latest'
        }

        & gh @editArguments
        if ($LASTEXITCODE -ne 0) {
            throw "GitHub draft release publication failed with exit code $LASTEXITCODE."
        }

        Write-Output "Published GitHub release $tag."
        return
    }

    $workingTreeChanges = @(& git status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "Could not inspect the Git working tree. Git exited with code $LASTEXITCODE."
    }
    if ($workingTreeChanges.Count -ne 0) {
        throw 'Refusing to create a release from a working tree with uncommitted changes.'
    }

    if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
        throw "Installer not found: $installerPath. Build it with .\installer\build-installer.ps1 -Version $Version"
    }

    $resolvedNotesFile = $null
    if (-not [string]::IsNullOrWhiteSpace($NotesFile)) {
        $notesFilePath = if ([System.IO.Path]::IsPathRooted($NotesFile)) {
            $NotesFile
        }
        else {
            Join-Path $repoRoot $NotesFile
        }
        $resolvedNotesFile = [System.IO.Path]::GetFullPath($notesFilePath)
        if (-not (Test-Path -LiteralPath $resolvedNotesFile -PathType Leaf)) {
            throw "Release notes file not found: $resolvedNotesFile"
        }
    }

    $targetCommit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($targetCommit)) {
        throw 'Could not resolve the release commit.'
    }

    $installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $installerName = Split-Path -Leaf $installerPath
    [System.IO.File]::WriteAllText(
        $checksumPath,
        "$installerHash  $installerName$([Environment]::NewLine)",
        [System.Text.UTF8Encoding]::new($false))

    $createArguments = @(
        'release', 'create', $tag,
        $installerPath,
        $checksumPath,
        '--target', $targetCommit,
        '--title', $releaseTitle,
        '--fail-on-no-commits'
    )
    if ($null -ne $resolvedNotesFile) {
        $createArguments += @('--notes-file', $resolvedNotesFile)
    }
    else {
        $createArguments += @('--notes', "Windows x64 installer for OKF-Todo $Version.")
    }
    if ($Draft) {
        $createArguments += '--draft'
    }
    elseif ($Latest) {
        $createArguments += '--latest'
    }

    & gh @createArguments
    if ($LASTEXITCODE -ne 0) {
        throw "GitHub release creation failed with exit code $LASTEXITCODE."
    }

    Write-Output "Created GitHub release $tag for commit $targetCommit."
    Write-Output "Installer: $installerPath"
    Write-Output "SHA-256: $checksumPath"
}
finally {
    Pop-Location
}
