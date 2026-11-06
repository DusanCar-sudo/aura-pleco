@echo off
setlocal
echo ========================================================
echo       🐟 PLECO FOR WINDOWS — BUILD SCRIPT
echo ========================================================

set CSC=""
if exist "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
) else if exist "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if %CSC%=="" (
    echo [!] Could not locate .NET Framework csc.exe compiler.
    echo Trying dotnet CLI instead...
    where dotnet >nul 2>&1
    if %errorlevel% equ 0 (
        dotnet build -c Release
        goto :done
    )
    echo [x] Error: Neither csc.exe nor dotnet SDK was found.
    pause
    exit /b 1
)

echo [*] Compiling Pleco.cs using built-in Windows C# compiler:
echo     %CSC%
%CSC% /target:exe /out:pleco.exe /platform:anycpu /optimize+ Pleco.cs

if exist pleco.exe (
    echo [+] SUCCESS: pleco.exe generated successfully!
    echo     You can now run:
    echo       pleco.exe --stream   (Live fish tank UI)
    echo       pleco.exe --once     (Single quick clean)
    echo       install.bat          (Install as background daemon)
) else (
    echo [x] Compilation failed.
)

:done
pause
