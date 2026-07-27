@echo off
setlocal

cd /d "%~dp0"
set "PROJECT=Okf-Todo\Okf-Todo.csproj"

tasklist /FI "IMAGENAME eq Okf-Todo.exe" /NH 2>nul | find /I "Okf-Todo.exe" >nul
if not errorlevel 1 (
    echo ERROR: OKF-Todo is already running.
    echo Close the existing application before building and starting another instance.
    exit /b 2
)

echo.
echo [1/4] Cleaning the Release build...
dotnet clean "%PROJECT%" -c Release
if errorlevel 1 goto :failed

echo.
echo [2/4] Restoring dependencies...
dotnet restore "%PROJECT%"
if errorlevel 1 goto :failed

echo.
echo [3/4] Building the Release application...
dotnet build "%PROJECT%" -c Release --no-restore
if errorlevel 1 goto :failed

echo.
echo [4/4] Starting OKF-Todo...
dotnet run --project "%PROJECT%" -c Release --no-build
if errorlevel 1 goto :failed

exit /b 0

:failed
echo.
echo ERROR: The operation failed. Review the output above.
exit /b 1
