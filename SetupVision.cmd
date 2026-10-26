@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\SetupVision.ps1" %*
set "SETUP_RESULT=%ERRORLEVEL%"
if not "%SETUP_RESULT%"=="0" pause
exit /b %SETUP_RESULT%
