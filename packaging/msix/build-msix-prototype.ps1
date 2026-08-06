[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0.0',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Install,

    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$artifactRoot = Join-Path $repoRoot 'artifacts\msix'
$packageRoot = Join-Path $artifactRoot 'package-root'
$outputRoot = Join-Path $artifactRoot 'output'
$certificateRoot = Join-Path $artifactRoot 'certificate'
$manifestPath = Join-Path $packageRoot 'Package.appxmanifest'
$certificatePath = Join-Path $certificateRoot 'Okf-Todo-MsixPrototype-dev.pfx'
$publicCertificatePath = Join-Path $certificateRoot 'Okf-Todo-MsixPrototype-dev.cer'
$certificatePasswordPath = Join-Path $certificateRoot 'Okf-Todo-MsixPrototype-dev-password.clixml'
$packagePath = Join-Path $outputRoot "Okf-Todo-$Version-win-x64-prototype.msix"
$packageName = 'OkfTodo.MsixPrototype'
$publisher = 'CN=OKF-Todo MSIX Prototype'
$executionAlias = 'okf-todo-msix-preview.exe'
$packagingSafetyScript = Join-Path $repoRoot 'packaging\packaging-safety.ps1'
. $packagingSafetyScript

function Reset-GeneratedDirectory {
    param([Parameter(Mandatory)][string]$Path)

    $resolvedArtifactRoot = [System.IO.Path]::GetFullPath($artifactRoot)
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith(
        $resolvedArtifactRoot + [System.IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the MSIX artifact root: $resolvedPath"
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
        throw "Required MSIX file is missing: $Path"
    }
}

function ConvertTo-PlainText {
    param([Parameter(Mandatory)][securestring]$SecureString)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Install-DevelopmentCertificate {
    param([Parameter(Mandatory)][string]$Path)

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($Path)
    $trustedCertificate = Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' |
        Where-Object Thumbprint -eq $certificate.Thumbprint
    if ($null -ne $trustedCertificate) {
        return
    }

    Write-Host 'Administrator approval is required to trust the local development certificate...'
    $escapedPath = $Path.Replace("'", "''")
    $importCommand = "Import-Certificate -FilePath '$escapedPath' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null"
    $encodedCommand = [Convert]::ToBase64String(
        [Text.Encoding]::Unicode.GetBytes($importCommand))
    $process = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList @(
            '-NoProfile',
            '-NonInteractive',
            '-EncodedCommand',
            $encodedCommand) `
        -Verb RunAs `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Development-certificate trust failed with exit code $($process.ExitCode)."
    }

    $trustedCertificate = Get-ChildItem 'Cert:\LocalMachine\TrustedPeople' |
        Where-Object Thumbprint -eq $certificate.Thumbprint
    if ($null -eq $trustedCertificate) {
        throw 'The development certificate was not found in LocalMachine\TrustedPeople after administrator approval.'
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
    New-Item -ItemType Directory -Path $certificateRoot -Force | Out-Null

    Write-Host 'Publishing the existing self-contained win-x64 payload...'
    & (Join-Path $repoRoot 'installer\build-installer.ps1') `
        -Version (($Version -split '\.')[0..2] -join '.') `
        -Configuration $Configuration `
        -SkipInstallerCompile
    if ($LASTEXITCODE -ne 0) {
        throw "Installer staging failed with exit code $LASTEXITCODE."
    }

    $installerStaging = Join-Path $repoRoot 'artifacts\installer\staging'
    Copy-DirectoryContents `
        -Source (Join-Path $installerStaging 'core') `
        -Destination $packageRoot
    Copy-DirectoryContents `
        -Source (Join-Path $installerStaging 'okf') `
        -Destination (Join-Path $packageRoot 'okf')
    Copy-DirectoryContents `
        -Source (Join-Path $installerStaging 'integration') `
        -Destination (Join-Path $packageRoot 'integration')

    Assert-FileExists -Path (Join-Path $packageRoot 'Okf-Todo.exe')
    Assert-FileExists -Path (Join-Path $packageRoot 'lookup-seed.json')
    Assert-FileExists -Path (Join-Path $packageRoot 'wwwroot\index.html')
    Assert-FileExists -Path (Join-Path $packageRoot 'wwwroot\help\using-okf-todo.md')
    Assert-FileExists -Path (Join-Path $packageRoot 'okf\todo-database\index.md')

    Write-Host 'Generating the development manifest and visual assets...'
    Push-Location $packageRoot
    try {
        & $winApp.Source manifest generate . `
            --package-name $packageName `
            --publisher-name $publisher `
            --version $Version `
            --description 'OKF-Todo local MSIX feasibility prototype' `
            --executable 'Okf-Todo.exe' `
            --logo-path (Join-Path $packageRoot 'wwwroot\favicon.ico') `
            --template Packaged `
            --if-exists Overwrite
        if ($LASTEXITCODE -ne 0) {
            throw "MSIX manifest generation failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }

    [xml]$manifest = Get-Content -LiteralPath $manifestPath -Raw
    $namespaceManager = [System.Xml.XmlNamespaceManager]::new($manifest.NameTable)
    $namespaceManager.AddNamespace(
        'foundation',
        'http://schemas.microsoft.com/appx/manifest/foundation/windows10')
    $namespaceManager.AddNamespace(
        'uap',
        'http://schemas.microsoft.com/appx/manifest/uap/windows10')

    $displayName = $manifest.SelectSingleNode(
        '/foundation:Package/foundation:Properties/foundation:DisplayName',
        $namespaceManager)
    $displayName.InnerText = 'OKF-Todo MSIX Prototype'

    $visualElements = $manifest.SelectSingleNode(
        '/foundation:Package/foundation:Applications/foundation:Application/uap:VisualElements',
        $namespaceManager)
    $visualElements.SetAttribute('DisplayName', 'OKF-Todo MSIX Prototype')
    $visualElements.SetAttribute('AppListEntry', 'none')

    $writerSettings = [System.Xml.XmlWriterSettings]::new()
    $writerSettings.Indent = $true
    $writerSettings.Encoding = [System.Text.UTF8Encoding]::new($false)
    $writer = [System.Xml.XmlWriter]::Create($manifestPath, $writerSettings)
    try {
        $manifest.Save($writer)
    }
    finally {
        $writer.Dispose()
    }

    & $winApp.Source manifest add-alias `
        --manifest $manifestPath `
        --name $executionAlias
    if ($LASTEXITCODE -ne 0) {
        throw "MSIX execution-alias generation failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $certificatePath -PathType Leaf)) {
        Write-Host 'Generating a local-only development certificate...'
        $randomPasswordBytes = New-Object byte[] 32
        $randomNumberGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
        try {
            $randomNumberGenerator.GetBytes($randomPasswordBytes)
        }
        finally {
            $randomNumberGenerator.Dispose()
        }
        $certificatePassword = [Convert]::ToBase64String($randomPasswordBytes)
        ConvertTo-SecureString $certificatePassword -AsPlainText -Force |
            Export-Clixml -LiteralPath $certificatePasswordPath

        & $winApp.Source cert generate `
            --manifest $manifestPath `
            --output $certificatePath `
            --password $certificatePassword `
            --valid-days 365 `
            --export-cer `
            --if-exists Skip
        if ($LASTEXITCODE -ne 0) {
            throw "Development certificate generation failed with exit code $LASTEXITCODE."
        }
    }
    else {
        Assert-FileExists -Path $certificatePasswordPath
        $certificatePassword = ConvertTo-PlainText (
            Import-Clixml -LiteralPath $certificatePasswordPath)
    }

    Assert-NoPackagedDatabaseFiles -Path $packageRoot

    Write-Host 'Packing and signing the local MSIX prototype...'
    & $winApp.Source pack $packageRoot `
        --manifest $manifestPath `
        --cert $certificatePath `
        --cert-password $certificatePassword `
        --output $packagePath `
        --exe 'Okf-Todo.exe' `
        --skip-pri
    if ($LASTEXITCODE -ne 0) {
        throw "MSIX packaging failed with exit code $LASTEXITCODE."
    }

    Assert-FileExists -Path $packagePath

    if ($Install) {
        Assert-FileExists -Path $publicCertificatePath
        Install-DevelopmentCertificate -Path $publicCertificatePath

        $installedPackage = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
        if ($null -ne $installedPackage -and [version]$installedPackage.Version -eq [version]$Version) {
            Write-Host "MSIX prototype version $Version is already installed."
        }
        else {
            Write-Host "Installing MSIX prototype version $Version..."
            Add-AppxPackage `
                -Path $packagePath `
                -ForceApplicationShutdown `
                -ForceUpdateFromAnyVersion
        }
    }

    Write-Host "MSIX prototype created at $packagePath"
    Write-Host "Development certificate: $certificatePath"
    Write-Host "Execution alias: $executionAlias"

    if ($Launch) {
        if (-not $Install) {
            throw '-Launch requires -Install.'
        }

        & (Join-Path $PSScriptRoot 'start-msix-prototype.ps1')
    }
}
finally {
    Pop-Location
}
