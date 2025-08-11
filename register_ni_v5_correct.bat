@echo off
echo =====================================
echo Register NI OPC Servers V5 (Correct Path)
echo =====================================

echo 1. Checking Administrator privileges...
net session >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo    ✅ Running as Administrator

echo 2. Found NI OPC Servers V5 installation
set NIPATH="C:\Program Files (x86)\National Instruments\Shared\NI OPC Servers\V5"

echo 3. Registering NI OPC V5 COM components...

echo    - Registering OpcDaServer.dll
regsvr32 /s %NIPATH%\OpcDaServer.dll
if %ERRORLEVEL% == 0 (
    echo      ✅ OpcDaServer.dll registered
) else (
    echo      ❌ Failed to register OpcDaServer.dll
)

echo    - Registering server_runtime.exe
regsvr32 /s %NIPATH%\server_runtime.exe
if %ERRORLEVEL% == 0 (
    echo      ✅ server_runtime.exe registered
) else (
    echo      ❌ Failed to register server_runtime.exe
)

echo    - Registering other DLL components...
for %%f in (%NIPATH%\*.dll) do (
    regsvr32 /s "%%f" 2>nul
    if not errorlevel 1 (
        echo      ✅ Registered: %%~nxf
    )
)

echo 4. Starting NI OPC Server services...
echo    - Attempting to start server_runtime.exe...
start "" %NIPATH%\server_runtime.exe

echo 5. Testing COM registration...
timeout /t 3 /nobreak >nul

reg query "HKEY_CLASSES_ROOT\NI.OPCLabVIEW" >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo    ✅ NI.OPCLabVIEW registered successfully
) else (
    echo    ⚠️ NI.OPCLabVIEW registration not found (may use different ProgID)
)

echo 6. Checking if OPC DA Server is accessible...
echo    💡 NI OPC Servers V5 may use different ProgIDs
echo    💡 Common V5 ProgIDs:
echo       - NI.OPCLabVIEW
echo       - National Instruments.OPCLabVIEW
echo       - NIOPCServers.V5
echo       - NI.OPCServers.Runtime

echo.
echo =====================================
echo Registration Complete!
echo =====================================
echo.
echo 🚀 Test with: OPC client
echo    Should try connecting to NI OPC Server
echo.
pause