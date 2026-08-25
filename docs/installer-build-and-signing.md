# Windows installer: build, sign, and publish

The installer can be built **unsigned** for development or **signed** with the
Certum certificate for public distribution.

> These scripts create the Inno Setup installer, not the Microsoft Store MSIX
> package. Partner Center signs the Store package separately.

## Which script should I use?

| Goal | Script |
| --- | --- |
| Build an installer | `installer/build-installer.ps1` |
| Build and publish the next GitHub alpha release | `installer/update_release_exe.ps1` |
| Publish an installer that is already built | `installer/publish-github-release.ps1` |
| Run the build-and-release shortcut | `build_all.ps1` |

`build-installer.ps1` also loads `packaging/packaging-safety.ps1`. The safety
checks prevent a user database from being packaged or managed by the installer.

## Build an unsigned installer

Use this for development:

```powershell
cd C:\git\Okf-Todo
.\installer\build-installer.ps1 -Version 0.2.0
```

The installer is written to `artifacts/installer`.

## Build a signed installer

First ensure SimplySign Desktop is running and connected. Then supply both the
SignTool path and certificate thumbprint:

```powershell
cd C:\git\Okf-Todo

$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = '4C7319FF0FDFC7778CDAC36A40154CA82C1D1B37'

.\installer\build-installer.ps1 `
  -Version 0.2.0 `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

This signs both `Okf-Todo.exe` and the completed installer.

## Use `update_release_exe.ps1`

The script builds the installer and immediately creates a GitHub release. Use
`-WhatIf` to preview the tag, version, and stable download name without building
or publishing:

```powershell
.\installer\update_release_exe.ps1 -Tag v0.2.0-alpha -WhatIf
```

Omit `-Tag` to let the script select the next alpha patch version.

### Publish without a certificate

This is supported, but is intended only when an unsigned public installer is
acceptable:

```powershell
.\installer\update_release_exe.ps1 -Tag v0.2.0-alpha
```

### Publish with the Certum certificate

SimplySign Desktop must be running and connected. This example loads the saved
user-level thumbprint directly, so it also works in a PowerShell window that was
already open when the variable was saved:

```powershell
$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = [Environment]::GetEnvironmentVariable(
  'OKF_TODO_SIGNING_CERTIFICATE_THUMBPRINT',
  'User')

if ([string]::IsNullOrWhiteSpace($thumbprint)) {
  throw 'The signing certificate thumbprint is not configured.'
}

.\installer\update_release_exe.ps1 `
  -Tag v0.2.0-alpha `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

`publish-github-release.ps1` only publishes an existing installer. It creates a
SHA-256 checksum but does not sign the file.

### Replace an existing release asset

By default, `update_release_exe.ps1` refuses to reuse a release tag. To rebuild
and replace only the same-named installer asset while preserving the existing
release and tag, add the explicit replacement switch:

```powershell
.\installer\update_release_exe.ps1 `
  -Tag v0.2.0-alpha `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl' `
  -ReplaceExistingAsset
```

`-ReplaceExistingAsset` requires an explicit existing `-Tag`. It uses GitHub's
`--clobber` behavior, which deletes the old asset before uploading the new one.
The script checks the release first and stops before rebuilding if GitHub has
made it immutable.

An immutable release and its assets cannot be changed. Publish a corrected
installer under a new tag instead:

```powershell
.\installer\update_release_exe.ps1 `
  -Tag v0.2.1-alpha `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

## Remember the thumbprint

The thumbprint identifies the public certificate. It is not a password or
private key. Save it once as a user environment variable:

```powershell
[Environment]::SetEnvironmentVariable(
  'OKF_TODO_SIGNING_CERTIFICATE_THUMBPRINT',
  '4C7319FF0FDFC7778CDAC36A40154CA82C1D1B37',
  'User')
```

Open a new PowerShell window. The scripts do not read this variable
automatically, so pass its value explicitly:

```powershell
$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = $env:OKF_TODO_SIGNING_CERTIFICATE_THUMBPRINT

.\installer\build-installer.ps1 `
  -Version 0.2.0 `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

## Keep certificate material out of Git

Never commit private keys, `.pfx` or `.p12` files, SimplySign tokens, PINs,
activation codes, or passwords. The downloaded `.cer` and `.pem` public
certificate files are not required by these scripts and should also remain
outside the repository.
