# Microsoft Store package

This is the production Store packaging path for the reserved **OKF-Todo**
product. It is independent of both the Inno Setup installer and the locally
signed MSIX feasibility prototype.

For the complete repeatable workflow—including version selection, testing,
Partner Center submission, `runFullTrust`, metadata-only changes, certification
failures, rollout recovery, and post-publication checks—use the
[Microsoft Store release runbook](RELEASE-RUNBOOK.md).

## Reserved Store identity

| Property | Value |
| --- | --- |
| Store ID | `9PP5FM2933BR` |
| Package identity name | `SrenDalby.OKF-Todo` |
| Publisher | `CN=663443C5-B2D3-4685-B52C-3CBCB8B68071` |
| Publisher display name | `Søren Dalby` |
| Package family name | `SrenDalby.OKF-Todo_0h80bfdm231km` |
| Execution alias | `okf-todo.exe` |

The package identity name and publisher are assigned by Partner Center. Do not
rename or normalize them. The user-facing Store and Start-menu name remains
**OKF-Todo**.

## Build the Store artifact

Install Microsoft's lightweight Windows App Development CLI once:

```powershell
winget install -e --id Microsoft.WinAppCli --source winget
```

Then run from the repository root:

```powershell
.\packaging\msix\build-msix-store.ps1 -Version 0.1.0.0
```

The upload artifact is written to:

```text
artifacts\msix-store\output\Okf-Todo-0.1.0.0-win-x64-store.msix
```

The script deliberately does not accept a certificate and does not install the
package. The exact Partner Center identity is embedded, and the package remains
unsigned until Microsoft signs it after Store certification. Upload this `.msix`
in the product's **Packages** submission page.

Every later Store submission must use a higher four-part package version. Keep
the identity values in `store-identity.psd1` unchanged.

## Data and MCP behavior

The Store package contains application files only. Construction fails if a
SQLite database enters the payload. Install, update, repair, and uninstall do
not manage `%LOCALAPPDATA%\Okf-Todo\okf-todo.db`, so the Store and Inno builds
use the same external local-first database without replacing it.

The Store app advertises `okf-todo.exe` as a stable execution alias. Its bundled
`integration\mcp-config.json` starts `okf-todo.exe --mcp`, avoiding a versioned
WindowsApps path while retaining the complete MCP interface.

## Before submission

1. Install the Windows SDK component that contains the Windows App Certification Kit.
2. Run the Certification Kit against a locally installable development build.
3. Create a Partner Center submission and upload the generated Store `.msix`.
4. Complete properties, age ratings, availability, privacy declarations, and Store listing artwork/text.
5. Submit for certification. Microsoft signs the accepted Store package; no purchased code-signing certificate is required for this path.

The reserved product page is <https://apps.microsoft.com/detail/9PP5FM2933BR>.
