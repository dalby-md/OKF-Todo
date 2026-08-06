[CmdletBinding()]
param(
    [string]$DatabasePath = (Join-Path `
        $env:LOCALAPPDATA `
        'Okf-Todo\MsixPrototype\okf-todo.db'),

    [switch]$SeedSampleData,

    [switch]$Wait
)

$ErrorActionPreference = 'Stop'
$packageName = 'OkfTodo.MsixPrototype'
$aliasPath = Join-Path `
    $env:LOCALAPPDATA `
    'Microsoft\WindowsApps\okf-todo-msix-preview.exe'

if ($null -eq (Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue)) {
    throw 'The OKF-Todo MSIX prototype is not installed. Run build-msix-prototype.ps1 -Install first.'
}

$databasePathRoot = [System.IO.Path]::GetPathRoot($DatabasePath)
$isDriveRelativePath = $databasePathRoot -match '^[A-Za-z]:$'
$isCurrentDriveRootedPath = $databasePathRoot -eq [System.IO.Path]::DirectorySeparatorChar.ToString()
if (-not [System.IO.Path]::IsPathRooted($DatabasePath) -or
    [string]::IsNullOrWhiteSpace($databasePathRoot) -or
    $isDriveRelativePath -or
    $isCurrentDriveRootedPath) {
    throw 'DatabasePath must be an absolute file path.'
}

$resolvedDatabasePath = [System.IO.Path]::GetFullPath($DatabasePath)
$databaseDirectory = Split-Path -Parent $resolvedDatabasePath
New-Item -ItemType Directory -Path $databaseDirectory -Force | Out-Null

$argumentList = @(
    '--database-path',
    ('"{0}"' -f $resolvedDatabasePath)
)
if ($SeedSampleData) {
    $argumentList += '--seed-sample-tasks'
}

$process = Start-Process `
    -FilePath $aliasPath `
    -ArgumentList $argumentList `
    -PassThru `
    -Wait:$Wait

Write-Host "Started OKF-Todo MSIX prototype with PID $($process.Id)."
Write-Host "Isolated database: $resolvedDatabasePath"
