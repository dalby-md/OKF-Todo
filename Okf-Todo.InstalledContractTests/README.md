# Installed contract tests

These Windows-only black-box tests exercise only an installed OKF-Todo product:

- `Okf-Todo.exe --okf-command`
- `mcp\Okf-Todo.Mcp.exe`
- `okf\todo-database\index.md` and its installed context files
- disposable SQLite databases created under the test runner's temporary directory

The project has no references to OKF-Todo application projects. It must not use repository documentation, publish output, installer staging, or the user's normal database as product context.

The suite contains three tracks:

1. MCP task insertion and replacement updates through the official .NET MCP client.
2. Supported OKF command insertion and replacement updates, including adding an attachment.
3. OKF-guided direct SQLite insertion and updates, including a BLOB attachment. These tests explicitly prove that direct database writes bypass automatic application history.

## Run against the default installation

```cmd
dotnet test .\Okf-Todo.InstalledContractTests\Okf-Todo.InstalledContractTests.csproj -c Release
```

The default installation root is `%LOCALAPPDATA%\Programs\Okf-Todo`.

## Run against another installation directory

```cmd
set OKF_TODO_INSTALL_DIR=C:\Path\To\Installed\Okf-Todo
dotnet test .\Okf-Todo.InstalledContractTests\Okf-Todo.InstalledContractTests.csproj -c Release
```

The MCP installer component is mandatory. Missing installed files fail environment validation rather than skipping tests.
