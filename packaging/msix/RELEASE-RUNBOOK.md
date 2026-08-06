# Microsoft Store release runbook

Use this runbook for every public Microsoft Store release of **OKF-Todo**. The
Store package is independent of the Inno Setup installer and the locally signed
MSIX prototype.

## Fixed product information

Do not change these values unless Partner Center explicitly assigns replacements:

| Property | Value |
| --- | --- |
| Product | `OKF-Todo` |
| Store ID | `9PP5FM2933BR` |
| Package identity name | `SrenDalby.OKF-Todo` |
| Publisher | `CN=663443C5-B2D3-4685-B52C-3CBCB8B68071` |
| Package family name | `SrenDalby.OKF-Todo_0h80bfdm231km` |
| Execution alias | `okf-todo.exe` |
| Public Store page | <https://apps.microsoft.com/detail/9PP5FM2933BR> |

The identity and publisher are public package metadata, not secrets. Passwords,
Microsoft account recovery details, Partner Center session data, access tokens,
local development certificates, and generated packages must never be committed.
Generated output is kept below the ignored `artifacts` directory.

## Version rule

MSIX versions contain four numeric parts:

```text
major.minor.patch.revision
```

Use the product release version in the first three parts and normally keep the
revision at zero:

| Release | MSIX version |
| --- | --- |
| First alpha | `0.1.0.0` |
| Next feature or fix release | `0.1.1.0` |
| Next minor release | `0.2.0.0` |
| First stable release | `1.0.0.0` |

Every uploaded replacement package must have a version higher than every
previously published package for the same architecture. Never reuse or decrease
a package version. If a package must be rebuilt after upload, increment the
revision, for example from `0.1.1.0` to `0.1.1.1`.

Keep the values in `store-identity.psd1` unchanged. Updating within the same
package family is what lets Windows recognize a new package as an update.

## One-time prerequisites

1. Keep access to the Microsoft account that owns the Partner Center developer
   account. Do not put its password or recovery information in this repository.
2. Install the .NET SDK required by the solution.
3. Install Microsoft's Windows App Development CLI:

   ```powershell
   winget install -e --id Microsoft.WinAppCli --source winget
   ```

4. Install the Windows SDK component containing the Windows App Certification
   Kit when local certification testing is required.
5. Keep at least one suitable desktop screenshot available under `docs/images`
   or another version-controlled artwork directory.

The Store upload package is intentionally unsigned. Microsoft signs it after
certification, so the Store path does not require a purchased code-signing
certificate.

## Release checklist

### 1. Prepare the source

1. Choose the new four-part version and confirm it is higher than the last Store
   package version.
2. Review the changes intended for the release. Do not package unrelated or
   unfinished working-tree changes.
3. Update user-visible Help with every affected feature. Build output must
   contain exact copies of the canonical Markdown files under `docs/help`.
4. If the physical database design changed, include the reviewed EF Core
   migration and regenerate and validate the OKF database context.
5. Back up the normal user database before performing an upgrade smoke test.

### 2. Build and test

From the repository root:

```powershell
dotnet build -c Release
dotnet test .\Okf-Todo.Tests\Okf-Todo.Tests.csproj -c Release
```

When an installed-package change is involved, also run the applicable installed
contract tests described in `Okf-Todo.InstalledContractTests/README.md`.

Build the Store package with the selected version:

```powershell
.\packaging\msix\build-msix-store.ps1 -Version 0.1.1.0
```

Expected upload artifact:

```text
artifacts\msix-store\output\Okf-Todo-0.1.1.0-win-x64-store.msix
```

The build script publishes a self-contained `win-x64` application, copies the
desktop UI, canonical offline Help, OKF bundle, and MCP configuration, validates
the immutable Store identity and `okf-todo.exe` alias, and fails if a SQLite
database enters the package.

Do not install the unsigned Store artifact locally. Use the signed prototype
package for local install and upgrade testing:

```powershell
.\packaging\msix\build-msix-prototype.ps1 -Version 0.1.1.0 -Install
.\packaging\msix\start-msix-prototype.ps1
```

The prototype uses an isolated database and is not the Store product. Run the
Windows App Certification Kit against a locally installable build when the
payload, manifest, native dependencies, or permissions have changed.

### 3. Create the Partner Center submission

1. Open **Partner Center → Apps and games → OKF-Todo**.
2. Select **Start update** or **Create a new submission**.
3. Under **Packages**, upload the new Store `.msix` and wait until validation is
   complete. Confirm the displayed identity, architecture, and version.
4. Review **Pricing and availability**, **Properties**, **Age ratings**, and the
   privacy URL. Do not assume copied values are still current.
5. Update the **Store listing**:
   - description and feature list match the released application;
   - **What's new in this version** describes user-visible changes;
   - screenshots show the current UI and contain no private task data;
   - copyright, support URL, website, and privacy URL remain correct.
6. Complete **Submission options**, including the `runFullTrust` explanation and
   useful certification notes.
7. Review every section until Partner Center marks it complete, then select
   **Submit for certification**.

### 4. Explain `runFullTrust`

Keep `runFullTrust`. It is required because Photino runs OKF-Todo as a packaged
Win32 desktop application outside AppContainer. It does not request
administrator elevation.

Use this short explanation when Partner Center asks why it is required:

> OKF-Todo is a packaged Photino.NET Win32 app requiring runFullTrust to launch outside AppContainer. It uses only the signed-in user's standard permissions and never requests administrator elevation. Full trust supports its desktop window, localhost-only server, local SQLite data, file dialogs, attachments, backup/export, and local command-line and MCP interfaces. It installs no services or drivers, makes no machine-wide changes, and sends no task data to cloud services.

Suggested certification notes:

> No account, credentials, subscription, or external service is required. Launch OKF-Todo from the Start menu and create and save a task to exercise the main workflow. File dialogs can be tested through database backup or export and attachment selection. All application functionality works locally.

### 5. While certification is running

The submitted draft is normally locked. To change a package, description, or
image before publication, either:

- cancel the submission, edit it, and submit it again; or
- allow it to finish, then create another submission.

Cancel only for a serious problem because resubmission restarts certification.
Watch the certification report and preserve the exact failure message if the
submission is rejected.

### 6. Verify after publication

1. Wait until Partner Center reports **In the Store**.
2. Open the public Store page in a signed-out or private browser window and
   verify the title, description, screenshots, privacy link, and install button.
3. Install or update OKF-Todo from Microsoft Store on a test machine or Windows
   user profile.
4. Confirm:
   - the application launches from Start;
   - the existing database and tasks remain intact after update;
   - a task can be created, edited, completed, and recovered;
   - attachments, backup/export, and offline Help work;
   - `okf-todo.exe --mcp` resolves through the execution alias and the MCP help
     and core read/write workflow work as expected.
5. Record the published MSIX version, publication date, source commit, and any
   certification notes in the release or repository history.

## Common update scenarios

### Change only a screenshot or Store text

Create a new Partner Center submission, edit **Store listing**, and submit it for
certification. No new MSIX and no higher package version are required for a
metadata-only update.

If another submission is already in certification, wait for it to finish or
cancel it. A published listing cannot be edited in place without a submission.

### Release application code

Build and upload a new MSIX with a higher four-part version. Store updates are
delivered within the same package family; do not change the package identity or
publisher.

### Certification fails

1. Download or copy the certification report and identify whether the failure
   concerns the package, listing, policy, privacy declaration, or restricted
   capability.
2. Fix only the reported issue and any directly related defect.
3. Re-run the build and relevant tests.
4. If package contents changed after upload, use a higher revision number.
5. Create or update the submission and include concise reproduction or reviewer
   notes explaining the correction.

Do not remove `runFullTrust` merely to avoid its review. Removing it would require
an AppContainer-oriented redesign of the Photino host, local storage, localhost
server, command interface, and MCP execution model.

### A bad update is already published

- For a limited-risk update, use gradual package rollout so the percentage can
  be increased only after verification.
- If a gradual rollout exposes a problem, halt it. Halting prevents additional
  users from receiving that package but does not downgrade users who already
  received it.
- Build a corrected package with a higher version and submit it. Do not attempt
  to publish a lower package version as a rollback.
- For a severe issue, use Partner Center availability controls to stop new
  acquisitions while preparing the corrected release. Treat this as an
  emergency measure, not a normal rollback mechanism.

Package flights can be introduced later for a known tester group. They are most
useful when the Store release process becomes frequent; they still go through
certification.

## Data-safety rules

- The database is external to the MSIX at
  `%LOCALAPPDATA%\Okf-Todo\okf-todo.db`.
- Installation, update, repair, and uninstall must never replace or delete it.
- Never add `.db`, `.sqlite`, SQLite journal/WAL files, backups, sample user data,
  credentials, or tokens to a package.
- Database replacement is allowed only through an explicit user-controlled
  restore or reset workflow.
- Database migrations move forward at application startup. Database downgrades
  are unsupported, so make a backup before testing an older application build.

## Repository and artifact boundaries

Commit:

- packaging scripts and identity configuration;
- source, migrations, canonical Help, OKF context, and release documentation;
- deliberately prepared Store artwork that contains no private information.

Do not commit:

- `artifacts` output, `.msix`, `.appx`, `.msixupload`, or `.appxupload` files;
- local development `.pfx`, `.cer`, or encrypted password artifacts;
- Partner Center exports containing secrets or session data;
- user databases, backups, logs containing private task data, or screenshots with
  private task content.

## Official references

- [Publish an update to an MSIX app](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/publish-update-to-your-app-on-store)
- [Manage and update an app](https://learn.microsoft.com/en-us/windows/apps/publish/faq/manage-and-update-your-app)
- [App package update constraints](https://learn.microsoft.com/en-us/windows/msix/app-package-updates)
- [Store submission options and restricted capabilities](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msix/manage-submission-options)
- [App capability declarations](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations)
- [Package flights](https://learn.microsoft.com/en-us/windows/apps/publish/package-flights)

