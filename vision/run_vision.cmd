@echo off
setlocal
cd /d "%~dp0.."
set "VISION_PYTHON=%CD%\vision\.venv\Scripts\python.exe"
if not exist "%VISION_PYTHON%" (
    echo Python environment was not found: vision\.venv
    echo Follow the Python setup steps in README.md, then retry.
    pause
    exit /b 1
)
"%VISION_PYTHON%" "%CD%\vision\main.py" %*
set "VISION_RESULT=%ERRORLEVEL%"
if not "%VISION_RESULT%"=="0" (
    echo.
    echo Vision stopped with an error. Check the message above and README.md.
    pause
)
exit /b %VISION_RESULT%
