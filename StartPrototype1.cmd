@echo off
setlocal
set "SHOWCASE_APP=%~dp0"
if not exist "%SHOWCASE_APP%InteractiveShowcase.exe" set "SHOWCASE_APP=%~dp0Builds\WindowsPrototype1Release\"
if not exist "%SHOWCASE_APP%InteractiveShowcase.exe" (
    echo Prototype 1 release app missing. Use its complete distribution package.
    pause
    exit /b 1
)
if not exist "%SHOWCASE_APP%config\experience.json" (
    echo Prototype 1 experience settings missing.
    pause
    exit /b 1
)
if not exist "%~dp0vision\.venv\Scripts\python.exe" (
    echo Run SetupVision.cmd before starting Prototype 1.
    pause
    exit /b 1
)
start "" "%SHOWCASE_APP%InteractiveShowcase.exe"
call "%~dp0StartVision.cmd" --config "%SHOWCASE_APP%config\vision.json" %*
exit /b %ERRORLEVEL%
