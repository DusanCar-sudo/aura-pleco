@echo off
setlocal
echo ========================================================
echo       🐟 PLECO FOR WINDOWS — UNINSTALLER
echo ========================================================

echo [*] Stopping running Pleco instances...
taskkill /f /im pleco.exe >nul 2>&1

echo [*] Removing Scheduled Task...
schtasks /delete /tn "PlecoTankCleaner" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "PlecoTankCleaner" /f >nul 2>&1

set APP_DIR=%LOCALAPPDATA%\Pleco
if exist "%APP_DIR%" (
    echo [*] Removing application files from %APP_DIR%...
    rmdir /s /q "%APP_DIR%" >nul 2>&1
)

echo ========================================================
echo [+] Pleco has been completely uninstalled.
echo ========================================================
pause
