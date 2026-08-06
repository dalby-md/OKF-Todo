[CmdletBinding()]
param(
    [switch]$RemoveTestData,

    [switch]$RemoveDevelopmentCertificate
)

$ErrorActionPreference = 'Stop'
$packageName = 'OkfTodo.MsixPrototype'
$package = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue

if ($null -eq $package) {
    Write-Host 'The OKF-Todo MSIX prototype is not installed.'
}
else {
    Remove-AppxPackage -Package $package.PackageFullName
    Write-Host "Removed $($package.PackageFullName)."
}

if ($RemoveTestData) {
    $testDataPath = [System.IO.Path]::GetFullPath((Join-Path `
        $env:LOCALAPPDATA `
        'Okf-Todo\MsixPrototype'))
    $expectedRoot = [System.IO.Path]::GetFullPath((Join-Path `
        $env:LOCALAPPDATA `
        'Okf-Todo'))

    if (-not $testDataPath.StartsWith(
        $expectedRoot + [System.IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove an unexpected test-data directory: $testDataPath"
    }

    if (Test-Path -LiteralPath $testDataPath) {
        Remove-Item -LiteralPath $testDataPath -Recurse -Force
        Write-Host "Removed isolated prototype data at $testDataPath."
    }
}

if ($RemoveDevelopmentCertificate) {
    $legacyRootCertificates = Get-ChildItem 'Cert:\CurrentUser\Root' |
        Where-Object {
            $_.Subject -eq 'CN=OKF-Todo MSIX Prototype' -and
            $_.Issuer -eq 'CN=OKF-Todo MSIX Prototype'
        }
    foreach ($certificate in $legacyRootCertificates) {
        & certutil.exe -user -delstore Root $certificate.Thumbprint | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not remove legacy current-user root certificate $($certificate.Thumbprint)."
        }

        Write-Host "Removed legacy current-user root certificate $($certificate.Thumbprint)."
    }

    $machineStorePath = 'Cert:\LocalMachine\TrustedPeople'
    $machineCertificates = Get-ChildItem $machineStorePath |
        Where-Object {
            $_.Subject -eq 'CN=OKF-Todo MSIX Prototype' -and
            $_.Issuer -eq 'CN=OKF-Todo MSIX Prototype'
        }
    if ($machineCertificates.Count -gt 0) {
        Write-Host 'Administrator approval is required to remove the machine-wide development-certificate trust...'
        $thumbprints = @($machineCertificates | Select-Object -ExpandProperty Thumbprint)
        $removalStatements = $thumbprints |
            ForEach-Object {
                "Remove-Item -LiteralPath 'Cert:\LocalMachine\TrustedPeople\$_' -Force"
            }
        $encodedCommand = [Convert]::ToBase64String(
            [Text.Encoding]::Unicode.GetBytes(($removalStatements -join '; ')))
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
            throw "Development-certificate removal failed with exit code $($process.ExitCode)."
        }

        foreach ($thumbprint in $thumbprints) {
            Write-Host "Removed development certificate $thumbprint from $machineStorePath."
        }
    }
}
