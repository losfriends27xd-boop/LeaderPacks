@echo off
setlocal
echo Building Leader Packs for Ages of Conflict...
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET SDK is not installed. Install the .NET SDK, then run this file again.
  pause
  exit /b 1
)
dotnet restore
if errorlevel 1 goto failed
dotnet build -c Release
if errorlevel 1 goto failed
echo.
echo BUILD COMPLETE.
echo Copy bin\Release\net472\LeaderPacks.dll to your game's BepInEx\plugins folder.
pause
exit /b 0
:failed
echo.
echo BUILD FAILED. Please send a screenshot of the error.
pause
exit /b 1
