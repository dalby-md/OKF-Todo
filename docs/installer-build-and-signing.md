# Windows installer: build, sign, and publish

The installer can be built **unsigned** for development or **signed** with the
Certum certificate for public distribution.

> These scripts create the Inno Setup installer, not the Microsoft Store MSIX
> package. Partner Center signs the Store package separately.

Run the commands on this page from the repository root. See the
[README requirements](../README.md#requirements) for source-build prerequisites.

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

The Windows installer is a self-contained `win-x64` Inno Setup package. It installs the unified desktop/command/MCP executable and the OKF context graph.

Install Inno Setup 7 (or compatible Inno Setup 6), then run from the repository root:

```powershell
.\installer\build-installer.ps1 -Version 1.0.0
```

Or from Windows cmd:

```cmd
installer\build-installer.cmd -Version 1.0.0
```

The installer is written to:

```text
artifacts\installer\Okf-Todo-1.0.0-win-x64-setup.exe
```

## Validate the staging payload

To publish, merge, and validate the staging payload without compiling the setup executable:

```powershell
.\installer\build-installer.ps1 -Version 1.0.0 -SkipInstallerCompile
```

The desktop application, OKF command adapter, and MCP server are provided by the single payload staged under `artifacts\installer\staging\core`; the installed OKF bundle is staged under `artifacts\installer\staging\okf`.

## Build a signed installer

First ensure SimplySign Desktop is running and connected. Use the SignTool path
installed on this machine and the thumbprint of your current signing certificate.
The SDK path below is an example; see [Remember the thumbprint](#remember-the-thumbprint)
for saving your certificate selection. Set the version for the release being built:

```powershell
$version = '1.0.0'
$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = [Environment]::GetEnvironmentVariable(
  'OKF_TODO_SIGNING_CERTIFICATE_THUMBPRINT',
  'User')
if ([string]::IsNullOrWhiteSpace($thumbprint)) {
  throw 'The signing certificate thumbprint is not configured.'
}

.\installer\build-installer.ps1 `
  -Version $version `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

This signs both `Okf-Todo.exe` and the completed installer. Verify the setup
signature before publishing it:

```powershell
$installerPath = ".\artifacts\installer\Okf-Todo-$version-win-x64-setup.exe"
$signature = Get-AuthenticodeSignature -LiteralPath $installerPath
if ($signature.Status -ne 'Valid' -or
    $signature.SignerCertificate.Thumbprint -ne $thumbprint) {
  throw 'The installer does not have a valid signature from the selected certificate.'
}
```

The publisher creates a checksum but does not enforce this signature check;
complete it before creating a public release.

## Use `update_release_exe.ps1`

The script builds the installer and immediately creates a GitHub release. Use
`-WhatIf` to preview the tag, version, and stable download name without building
or publishing:

```powershell
.\installer\update_release_exe.ps1 -Tag v0.2.0-alpha -WhatIf
```

Install and authenticate the GitHub CLI before publishing:

```powershell
gh auth login
```

For a public alpha, use [Publish with the Certum certificate](#publish-with-the-certum-certificate). Omit `-Tag` in that signed command to select the next alpha patch automatically.

When `-Tag` is omitted, the script finds the highest existing `v<major>.<minor>.<patch>-alpha` release, increments its patch number, builds that installer version, copies it to the stable `<major>.<minor>` asset name, creates the new release, and marks it as GitHub's latest release. For example, `v0.1.4-alpha` produces `v0.1.5-alpha`, builds `Okf-Todo-0.1.5-win-x64-setup.exe`, and uploads it as `Okf-Todo-0.1-win-x64-setup.exe`.

The tag and title identify the build as alpha, but the GitHub release is intentionally not flagged as a prerelease. GitHub excludes prereleases from `/releases/latest`, so marking it as a prerelease would break the stable installer URL linked from the [README](../README.md#available-for-windows-macos-and-linux).

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
  '<your-current-certificate-thumbprint>',
  'User')
```

Open a new PowerShell window. The scripts do not read this variable
automatically, so pass its value explicitly:

```powershell
$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = $env:OKF_TODO_SIGNING_CERTIFICATE_THUMBPRINT

.\installer\build-installer.ps1 `
  -Version 1.0.0 `
  -SignToolPath $signTool `
  -CertificateThumbprint $thumbprint `
  -TimestampUrl 'http://time.certum.pl'
```

## Keep certificate material out of Git

Never commit private keys, `.pfx` or `.p12` files, SimplySign tokens, PINs,
activation codes, or passwords. The downloaded `.cer` and `.pem` public
certificate files are not required by these scripts and should also remain
outside the repository.

## Coordinate a Store and GitHub release

For a coordinated Store and GitHub launch, first complete [Build a signed
installer](#build-a-signed-installer), including its signature verification, for
the chosen version. Run the applicable [installed contract tests](../Okf-Todo.InstalledContractTests/README.md)
against the installed build. The example below assumes that version is `1.0.0`.
Create the GitHub draft from the exact source commit used for that build:

```powershell
.\installer\publish-github-release.ps1 `
  -Version 1.0.0 `
  -Tag v1.0.0 `
  -Title 'OKF Todo 1.0.0' `
  -NotesFile docs\release-notes\v1.0.0.md `
  -Draft
```

The publisher refuses a dirty working tree, targets the current commit, uploads
the versioned installer and its SHA-256 checksum, and leaves the release as a
draft while Store certification runs. After Partner Center certifies and holds
the Store submission, publish that tested draft as GitHub's latest release:

```powershell
.\installer\publish-github-release.ps1 `
  -Tag v1.0.0 `
  -PublishDraft `
  -Latest
```

Do not run `update_release_exe.ps1` and the coordinated draft workflow for the
same release. Follow the [Store release runbook](../packaging/msix/RELEASE-RUNBOOK.md)
for certification of the exact Store artifact and coordinated publication.

## Other Windows package formats

Use the [MSIX prototype guide](../packaging/msix/README.md) for isolated local
installation and upgrade testing. Use the [Microsoft Store package guide](../packaging/msix/STORE.md)
for package identity and build details, and the [Store release runbook](../packaging/msix/RELEASE-RUNBOOK.md)
for certification and publication. Those pages own their procedures.

All package paths keep application payloads separate from user databases.
