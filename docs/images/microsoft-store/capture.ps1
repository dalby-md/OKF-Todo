$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$previousOutput = $env:OKF_STORE_SCREENSHOT_OUTPUT
try {
    $env:OKF_STORE_SCREENSHOT_OUTPUT = $PSScriptRoot
    dotnet test (Join-Path $repoRoot 'Okf-Todo.UiTests/Okf-Todo.UiTests.csproj') -c Release --no-restore -p:StoreScreenshotCapture=true --filter FullyQualifiedName~CaptureMicrosoftStoreScreenshots
    if ($LASTEXITCODE -ne 0) { throw 'Store screenshot capture failed.' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '01-task-workspace.png') -Destination (Join-Path $PSScriptRoot '../okf-todo-task-workspace.png')
} finally {
    $env:OKF_STORE_SCREENSHOT_OUTPUT = $previousOutput
}
