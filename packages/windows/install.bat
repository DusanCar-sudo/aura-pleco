@echo off
setlocal
echo ========================================================
echo       🐟 PLECO FOR WINDOWS — 1-CLICK INSTALLER
echo ========================================================

:: Check for pleco.exe, compile if needed
if not exist "%~dp0pleco.exe" (
    echo [*] pleco.exe not found, compiling automatically...
    call "%~dp0build.bat"
)

if not exist "%~dp0pleco.exe" (
    echo [x] Error: pleco.exe could not be prepared.
    pause
    exit /b 1
)

set APP_DIR=%LOCALAPPDATA%\Pleco
echo [*] Installing Pleco to: %APP_DIR%
if not exist "%APP_DIR%" mkdir "%APP_DIR%"

copy /y "%~dp0pleco.exe" "%APP_DIR%\" >nul
copy /y "%~dp0PlecoConfig.json" "%APP_DIR%\" >nul 2>&1

echo [*] Creating Windows Scheduled Task: PlecoTankCleaner
schtasks /create /tn "PlecoTankCleaner" /tr "\"%APP_DIR%\pleco.exe\"" /sc onlogon /rl highest /f >nul 2>&1

if %errorlevel% neq 0 (
    echo [!] Elevated task creation failed, registering user logon run key instead...
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "PlecoTankCleaner" /t REG_SZ /d "\"%APP_DIR%\pleco.exe\"" /f >nul
)

echo [*] Launching Pleco in background...
start "" "%APP_DIR%\pleco.exe"

echo ========================================================
echo [+] VICTORY: Pleco is installed and swimming in your tank!
echo     - Runs silently on startup
echo     - Logs to: %APP_DIR%\pleco.log
echo     - Run "%APP_DIR%\pleco.exe --stream" anytime to watch it live!
echo ========================================================
pause
