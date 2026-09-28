# Windows installer: build, sign, and publish

The installer can be built **unsigned** for development or **signed** with the
Certum certificate for public distribution.

> These scripts create the Inno Setup installer, not the Microsoft Store MSIX
> package. Partner Center signs the Store package separately.
> 
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

First ensure SimplySign Desktop is running and connected. Then supply both the
SignTool path and certificate thumbprint:

```powershell
cd C:\git\Okf-Todo

$signTool = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe'
$thumbprint = '4C7319FF0FDFC7778CDAC36A40154CA82C1D1B37'

.\installer\build-installer.ps1 `
  -Version 1.0.0 `
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

Install and authenticate the GitHub CLI before publishing:

```powershell
gh auth login
```

To build and publish the next alpha release:

```powershell
.\installer\update_release_exe.ps1
```

With no parameters, the script finds the highest existing `v<major>.<minor>.<patch>-alpha` release, increments its patch number, builds that installer version, copies it to the stable `<major>.<minor>` asset name, creates the new release, and marks it as GitHub's latest release. For example, `v0.1.4-alpha` produces `v0.1.5-alpha`, builds `Okf-Todo-0.1.5-win-x64-setup.exe`, and uploads it as `Okf-Todo-0.1-win-x64-setup.exe`.

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
  '4C7319FF0FDFC7778CDAC36A40154CA82C1D1B37',
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

For a coordinated Store and GitHub launch, build the tested installer once and
create a GitHub draft for the exact release commit:

```powershell
.\installer\build-installer.ps1 -Version 1.0.0
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
same release.

## Build the local MSIX feasibility prototype

The experimental MSIX path is independent of Inno Setup and never publishes to
Microsoft Store. It reuses the same self-contained `win-x64` payload, signs it
with a local-only development certificate, and launches against an isolated
prototype database.

Both installer builds fail if a database file enters their staged payload. The
MSIX and Inno installers contain application files only and never install over
the user's database.

Install Microsoft's lightweight Windows App Development CLI, then build and
install the package:

```powershell
winget install -e --id Microsoft.WinAppCli --source winget
.\packaging\msix\build-msix-prototype.ps1 -Version 1.0.0.0 -Install
.\packaging\msix\start-msix-prototype.ps1
```

See [the MSIX prototype guide](../packaging/msix/README.md) for upgrade, sample-data,
and cleanup commands. The Inno installer remains the direct-download packaging
path.

## Build the Microsoft Store package

The Store build is separate from the local MSIX prototype and uses the immutable
identity reserved in Partner Center. It produces an unsigned `.msix`; Microsoft
signs the package after Store certification, so this path does not require a
purchased code-signing certificate.

```powershell
.\packaging\msix\build-msix-store.ps1 -Version 1.0.0.0
```

The artifact is written under `artifacts\msix-store\output`. See the
[Microsoft Store package guide](../packaging/msix/STORE.md) for the exact identity,
validation, versioning, data-safety, MCP-alias, and Partner Center handoff rules.
