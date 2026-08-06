# OKF-Todo MSIX feasibility prototype

This packaging path creates a real, locally installable MSIX without replacing
the production Inno Setup installer or publishing anything to Microsoft Store.

The prototype uses:

- the same self-contained `win-x64` payload as the Inno installer;
- package identity `OkfTodo.MsixPrototype`;
- a local-only self-signed development certificate;
- execution alias `okf-todo-msix-preview.exe`;
- an isolated database at
  `%LOCALAPPDATA%\Okf-Todo\MsixPrototype\okf-todo.db`.

The package is deliberately hidden from the Start menu. Launch it through the
provided script so it cannot accidentally open the normal OKF-Todo database.
Package construction fails if any SQLite database enters the payload. Installing,
upgrading, repairing, or uninstalling the package never overwrites the normal or
prototype database.

## Prerequisite

Install Microsoft's Windows App Development CLI:

```powershell
winget install -e --id Microsoft.WinAppCli --source winget
```

The full Windows SDK and Visual Studio are not required for this prototype.
The scripts support both Windows PowerShell 5.1 and PowerShell 7.

## Build and install

From the repository root:

```powershell
.\packaging\msix\build-msix-prototype.ps1 `
  -Version 0.1.0.0 `
  -Install
```

The package is written to:

```text
artifacts\msix\output\Okf-Todo-0.1.0.0-win-x64-prototype.msix
```

Generated packages, manifests, assets, and development certificates remain
under the ignored `artifacts` directory. The development certificate is only
for local testing, is trusted in the local computer's `TrustedPeople` store,
and must not be used for public distribution. The `-Install` command requests
administrator approval when that certificate trust must be added.

The PFX password is random and stored with Windows current-user encryption in
the adjacent `.clixml` artifact. Neither file is committed.

## Launch with isolated data

```powershell
.\packaging\msix\start-msix-prototype.ps1
```

To populate the isolated database with sample tasks before launching:

```powershell
.\packaging\msix\start-msix-prototype.ps1 -SeedSampleData -Wait
.\packaging\msix\start-msix-prototype.ps1
```

## Exercise an upgrade

Build and install a higher four-part version using the same development
certificate:

```powershell
.\packaging\msix\build-msix-prototype.ps1 `
  -Version 0.1.0.1 `
  -Install
```

Windows requires a higher package version for an in-place MSIX update.

## Remove the prototype

Preserve the isolated database:

```powershell
.\packaging\msix\uninstall-msix-prototype.ps1
```

Remove both the package and isolated prototype data:

```powershell
.\packaging\msix\uninstall-msix-prototype.ps1 -RemoveTestData
```

Remove the package, isolated data, and all prototype development-certificate
trust. Removing the machine-wide trust requests administrator approval:

```powershell
.\packaging\msix\uninstall-msix-prototype.ps1 `
  -RemoveTestData `
  -RemoveDevelopmentCertificate
```

The normal database at `%LOCALAPPDATA%\Okf-Todo\okf-todo.db` is never removed
by these scripts.
