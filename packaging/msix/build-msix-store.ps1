[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0.0',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$identity = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'store-identity.psd1')
$publisherDisplayName = $identity.PublisherDisplayNameFormat -f [char]0x00F8
$artifactRoot = Join-Path $repoRoot 'artifacts\msix-store'
$packageRoot = Join-Path $artifactRoot 'package-root'
$outputRoot = Join-Path $artifactRoot 'output'
$manifestPath = Join-Path $packageRoot 'Package.appxmanifest'
$packagePath = Join-Path $outputRoot "Okf-Todo-$Version-win-x64-store.msix"
$packagingSafetyScript = Join-Path $repoRoot 'packaging\packaging-safety.ps1'
. $packagingSafetyScript

function Reset-GeneratedDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $resolvedArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot).TrimEnd('\')
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith(
        $resolvedArtifactRoot + [System.IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the Store MSIX artifact root: $resolvedPath"
    }

    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resolvedPath -Force | Out-Null
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    Get-ChildItem -LiteralPath $Source -Force |
        Copy-Item -Destination $Destination -Recurse -Force
}

function Assert-FileExists {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required Store MSIX file is missing: $Path"
    }
}

function Save-XmlWithoutBom {
    param(
        [Parameter(Mandatory)][xml]$Document,
        [Parameter(Mandatory)][string]$Path
    )

    $writerSettings = [System.Xml.XmlWriterSettings]::new()
    $writerSettings.Indent = $true
    $writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($Path, $writerSettings)
    try {
        $Document.Save($writer)
    }
    finally {
        $writer.Dispose()
    }
}

function Assert-StoreManifest {
    param([Parameter(Mandatory)][string]$Path)

    [xml]$document = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($document.NameTable)
    $namespaceManager.AddNamespace('foundation', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $namespaceManager.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')
    $namespaceManager.AddNamespace('uap5', 'http://schemas.microsoft.com/appx/manifest/uap/windows10/5')
    $namespaceManager.AddNamespace('rescap', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities')

    $identityNode = $document.SelectSingleNode('/foundation:Package/foundation:Identity', $namespaceManager)
    $displayNameNode = $document.SelectSingleNode(
        '/foundation:Package/foundation:Properties/foundation:DisplayName',
        $namespaceManager)
    $publisherDisplayNameNode = $document.SelectSingleNode(
        '/foundation:Package/foundation:Properties/foundation:PublisherDisplayName',
        $namespaceManager)
    if ($identityNode.GetAttribute('Name') -ne $identity.PackageName -or
        $identityNode.GetAttribute('Publisher') -ne $identity.Publisher -or
        $identityNode.GetAttribute('Version') -ne $Version -or
        $displayNameNode.InnerText -ne $identity.DisplayName -or
        $publisherDisplayNameNode.InnerText -ne $publisherDisplayName) {
        throw 'The generated manifest does not match the immutable Partner Center package identity.'
    }

    $deviceFamily = $document.SelectSingleNode(
        '/foundation:Package/foundation:Dependencies/foundation:TargetDeviceFamily[@Name="Windows.Desktop"]',
        $namespaceManager)
    $runFullTrust = $document.SelectSingleNode(
        '/foundation:Package/foundation:Capabilities/rescap:Capability[@Name="runFullTrust"]',
        $namespaceManager)
    $executionAlias = $document.SelectSingleNode(
        '/foundation:Package/foundation:Applications/foundation:Application/foundation:Extensions/uap5:Extension/uap5:AppExecutionAlias/uap5:ExecutionAlias',
        $namespaceManager)
    if ($null -eq $deviceFamily -or $null -eq $runFullTrust) {
        throw 'The Store manifest must target Windows.Desktop and declare runFullTrust.'
    }
    if ($null -eq $executionAlias -or
        $executionAlias.GetAttribute('Alias') -ne $identity.ExecutionAlias) {
        throw 'The Store manifest does not contain the expected MCP execution alias.'
    }
}

$winApp = Get-Command winapp.exe -ErrorAction SilentlyContinue
if ($null -eq $winApp) {
    throw @'
Microsoft WinApp CLI is required. Install it with:
winget install -e --id Microsoft.WinAppCli --source winget
'@
}

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
    Reset-GeneratedDirectory -Path $packageRoot
    Reset-GeneratedDirectory -Path $outputRoot

    Write-Host 'Publishing the self-contained win-x64 Store payload...'
    & (Join-Path $repoRoot 'installer\build-installer.ps1') `
        -Version (($Version -split '\.')[0..2] -join '.') `
        -Configuration $Configuration `
        -SkipInstallerCompile
    if ($LASTEXITCODE -ne 0) {
        throw "Installer staging failed with exit code $LASTEXITCODE."
    }

    $installerStaging = Join-Path $repoRoot 'artifacts\installer\staging'
    Copy-DirectoryContents -Source (Join-Path $installerStaging 'core') -Destination $packageRoot
    Copy-DirectoryContents -Source (Join-Path $installerStaging 'okf') -Destination (Join-Path $packageRoot 'okf')
    Copy-DirectoryContents -Source (Join-Path $installerStaging 'integration') -Destination (Join-Path $packageRoot 'integration')

    Assert-FileExists -Path (Join-Path $packageRoot 'Okf-Todo.exe')
    Assert-FileExists -Path (Join-Path $packageRoot 'lookup-seed.json')
    Assert-FileExists -Path (Join-Path $packageRoot 'wwwroot\index.html')
    Assert-FileExists -Path (Join-Path $packageRoot 'wwwroot\help\using-okf-todo.md')
    Assert-FileExists -Path (Join-Path $packageRoot 'wwwroot\help\mcp-server.md')
    Assert-FileExists -Path (Join-Path $packageRoot 'okf\todo-database\index.md')

    Write-Host 'Generating the manifest with the immutable Partner Center identity...'
    Push-Location $packageRoot
    try {
        & $winApp.Source manifest generate . `
            --package-name $identity.PackageName `
            --publisher-name $identity.Publisher `
            --version $Version `
            --description $identity.Description `
            --executable 'Okf-Todo.exe' `
            --logo-path (Join-Path $packageRoot 'wwwroot\favicon.ico') `
            --template Packaged `
            --if-exists Overwrite
        if ($LASTEXITCODE -ne 0) {
            throw "Store manifest generation failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    [xml]$manifest = [System.IO.File]::ReadAllText(
        $manifestPath,
        [System.Text.Encoding]::UTF8)
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
    $namespaceManager.AddNamespace('foundation', 'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $namespaceManager.AddNamespace('uap', 'http://schemas.microsoft.com/appx/manifest/uap/windows10')

    $manifest.SelectSingleNode(
        '/foundation:Package/foundation:Properties/foundation:DisplayName',
        $namespaceManager).InnerText = $identity.DisplayName
    $manifest.SelectSingleNode(
        '/foundation:Package/foundation:Properties/foundation:PublisherDisplayName',
        $namespaceManager).InnerText = $publisherDisplayName

    $visualElements = $manifest.SelectSingleNode(
        '/foundation:Package/foundation:Applications/foundation:Application/uap:VisualElements',
        $namespaceManager)
    $visualElements.SetAttribute('DisplayName', $identity.DisplayName)
    $visualElements.SetAttribute('Description', $identity.Description)
    $visualElements.RemoveAttribute('AppListEntry')
    Save-XmlWithoutBom -Document $manifest -Path $manifestPath

    & $winApp.Source manifest add-alias --manifest $manifestPath --name $identity.ExecutionAlias
    if ($LASTEXITCODE -ne 0) {
        throw "Store execution-alias generation failed with exit code $LASTEXITCODE."
    }

    $mcpConfiguration = @{
        mcpServers = @{
            'okf-todo' = @{
                command = $identity.ExecutionAlias
                args = @('--mcp')
            }
        }
    } | ConvertTo-Json -Depth 4
    [System.IO.File]::WriteAllText(
        (Join-Path $packageRoot 'integration\mcp-config.json'),
        $mcpConfiguration + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))

    Assert-StoreManifest -Path $manifestPath
    Assert-NoPackagedDatabaseFiles -Path $packageRoot

    Write-Host 'Packing the unsigned Store MSIX...'
    & $winApp.Source pack $packageRoot `
        --manifest $manifestPath `
        --output $packagePath `
        --exe 'Okf-Todo.exe' `
        --skip-pri
    if ($LASTEXITCODE -ne 0) {
        throw "Store MSIX packaging failed with exit code $LASTEXITCODE."
    }

    Assert-FileExists -Path $packagePath
    $signature = Get-AuthenticodeSignature -LiteralPath $packagePath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::NotSigned) {
        throw "Expected an unsigned Store upload package, but signature status is $($signature.Status)."
    }

    Write-Host "Unsigned Store MSIX created at $packagePath"
    Write-Host "Store ID: $($identity.StoreId)"
    Write-Host "Execution alias: $($identity.ExecutionAlias)"
    Write-Host 'Do not install this unsigned artifact locally; upload it to Partner Center for certification and Microsoft signing.'
}
finally {
    Pop-Location
}
