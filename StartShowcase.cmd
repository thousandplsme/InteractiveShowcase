@echo off
setlocal
set "SHOWCASE_APP=%~dp0"
if not exist "%SHOWCASE_APP%InteractiveShowcase.exe" set "SHOWCASE_APP=%~dp0Builds\WindowsPrototype0Release\"
if not exist "%SHOWCASE_APP%InteractiveShowcase.exe" (
    echo Release app missing. Use the complete distribution package or build the release app first.
    pause
    exit /b 1
)
if not exist "%~dp0vision\.venv\Scripts\python.exe" (
    echo Run SetupVision.cmd before starting the showcase.
    pause
    exit /b 1
)
start "" "%SHOWCASE_APP%InteractiveShowcase.exe"
call "%~dp0StartVision.cmd" --config "%SHOWCASE_APP%config\vision.json" %*
exit /b %ERRORLEVEL%
