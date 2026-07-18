[CmdletBinding()]
param(
    [string]$Tag
)

$ErrorActionPreference = 'Stop'
$tagPattern = '^v\d+\.\d+\.\d+-alpha$'

if ([string]::IsNullOrWhiteSpace($Tag)) {
    $latestTag = gh release view --json tagName --jq '.tagName'

    if ($LASTEXITCODE -ne 0) {
        throw "Could not read the latest GitHub release tag. GitHub CLI exited with code $LASTEXITCODE."
    }

    $latestTag = $latestTag.Trim()
    $tagMatch = [regex]::Match($latestTag, '^v(\d+)\.(\d+)\.(\d+)-alpha$')

    if (-not $tagMatch.Success) {
        throw "Cannot calculate the next alpha tag from '$latestTag'."
    }

    $major = [int]$tagMatch.Groups[1].Value
    $minor = [int]$tagMatch.Groups[2].Value
    $patch = [int]$tagMatch.Groups[3].Value + 1
    $suggestedTag = "v$major.$minor.$patch-alpha"
    $enteredTag = Read-Host "New release tag [$suggestedTag]"

    $Tag = if ([string]::IsNullOrWhiteSpace($enteredTag)) {
        $suggestedTag
    }
    else {
        $enteredTag.Trim()
    }
}

if ($Tag -notmatch $tagPattern) {
    throw "Invalid release tag '$Tag'. Expected a tag such as v0.1.5-alpha."
}

$assetPath = Join-Path `
    $PSScriptRoot `
    '..\artifacts\installer\Okf-Todo-0.1-win-x64-setup.exe'

if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
    throw "Installer not found: $assetPath"
}

$assetPath = (Resolve-Path -LiteralPath $assetPath).Path
$displayVersion = $Tag -replace '^v', '' -replace '-alpha$', ' alpha'

gh release create $Tag `
    $assetPath `
    --title "OKF-Todo $displayVersion" `
    --notes 'Windows installer.' `
    --latest

if ($LASTEXITCODE -ne 0) {
    throw "GitHub release creation failed with exit code $LASTEXITCODE."
}

Write-Output "Release $Tag created."
Write-Output 'Stable installer URL:'
Write-Output 'https://github.com/dalby-md/OKF-Todo/releases/latest/download/Okf-Todo-0.1-win-x64-setup.exe'
