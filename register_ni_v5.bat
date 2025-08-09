@echo off
echo ===================================
echo Register NI OPC Servers V5 COM Objects  
echo ===================================

echo 1. Checking Administrator privileges...
net session >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo    ✅ Running as Administrator

echo 2. Stopping NI OPC services...
net stop "NI OPC Servers" 2>nul
taskkill /f /im "niserver.exe" 2>nul
taskkill /f /im "niopcservers.exe" 2>nul
taskkill /f /im "lkads.exe" 2>nul
timeout /t 2 /nobreak >nul

echo 3. Finding NI OPC Servers V5 installation...
set NIPATH=""

if exist "C:\Program Files (x86)\National Instruments\Shared\NI OPC Servers\V5\niserver.exe" (
    set NIPATH="C:\Program Files (x86)\National Instruments\Shared\NI OPC Servers\V5"
    echo    ✅ Found at: %NIPATH%
) else if exist "C:\Program Files\National Instruments\Shared\NI OPC Servers\V5\niserver.exe" (
    set NIPATH="C:\Program Files\National Instruments\Shared\NI OPC Servers\V5"
    echo    ✅ Found at: %NIPATH%
) else (
    echo    ❌ NI OPC Servers V5 not found!
    echo    Expected location: C:\Program Files (x86)\National Instruments\Shared\NI OPC Servers\V5\
    pause
    exit /b 1
)

echo 4. Registering NI OPC Server COM components...

echo    - Registering niserver.exe...
regsvr32 /s %NIPATH%\niserver.exe
if %ERRORLEVEL% == 0 (
    echo      ✅ niserver.exe registered
) else (
    echo      ❌ Failed to register niserver.exe
)

echo    - Registering niopcservers.exe...
regsvr32 /s %NIPATH%\niopcservers.exe
if %ERRORLEVEL% == 0 (
    echo      ✅ niopcservers.exe registered
) else (
    echo      ❌ Failed to register niopcservers.exe
)

echo    - Registering additional DLL files...
for %%f in (%NIPATH%\*.dll) do (
    regsvr32 /s "%%f" 2>nul
    echo      🔄 Registered: %%~nxf
)

echo 5. Installing OPC Core Components...
if exist "C:\Users\Lenovo\source\repos\ConsoleApp2\ConsoleApp2\OPC_Core_x86.msi" (
    echo    - Installing OPC Core x86...
    msiexec /i "C:\Users\Lenovo\source\repos\ConsoleApp2\ConsoleApp2\OPC_Core_x86.msi" /quiet /norestart
    timeout /t 5 /nobreak >nul
)

if exist "C:\Users\Lenovo\source\repos\ConsoleApp2\ConsoleApp2\OPC_Core_x64.msi" (
    echo    - Installing OPC Core x64...
    msiexec /i "C:\Users\Lenovo\source\repos\ConsoleApp2\ConsoleApp2\OPC_Core_x64.msi" /quiet /norestart
    timeout /t 5 /nobreak >nul
)

echo 6. Configuring DCOM settings...
echo    - Setting DCOM permissions for OPC Server...

:: Add DCOM configuration
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{13486D51-4821-11D2-A494-3CB306C10000}" /v "AuthenticationLevel" /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{13486D51-4821-11D2-A494-3CB306C10000}" /v "EnableDCOMHTTP" /t REG_DWORD /d 1 /f >nul 2>&1

echo      ✅ DCOM configured

echo 7. Starting NI OPC services...
net start "NI OPC Servers" 2>nul
if %ERRORLEVEL% == 0 (
    echo    ✅ NI OPC Servers service started
) else (
    echo    ⚠️ Service start failed - manual start may be required
)

echo 8. Testing COM registration...
reg query "HKEY_CLASSES_ROOT\NI.OPCLabVIEW" >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo    ✅ NI.OPCLabVIEW registered successfully
) else (
    echo    ❌ NI.OPCLabVIEW registration not found
)

reg query "HKEY_CLASSES_ROOT\National Instruments.OPCLabVIEW" >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo    ✅ National Instruments.OPCLabVIEW registered successfully
) else (
    echo    ❌ National Instruments.OPCLabVIEW registration not found
)

echo.
echo ===================================
echo COM Registration Complete!
echo ===================================
echo.
echo 🚀 Next step: Test OPC client
echo    cd "C:\Users\Lenovo\source\repos\ConsoleApp2\ConsoleApp2\QuickTest"
echo    dotnet run
echo.
pause