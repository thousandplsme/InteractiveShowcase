@echo off
setlocal
cd /d "%~dp0"
if not exist "vision\.venv\Scripts\python.exe" (
    echo Missing vision\.venv. Run SetupVision.cmd first.
    pause
    exit /b 1
)
"vision\.venv\Scripts\python.exe" "vision\doctor.py" %*
set "CHECK_RESULT=%ERRORLEVEL%"
if not "%~1"=="--no-pause" pause
exit /b %CHECK_RESULT%
