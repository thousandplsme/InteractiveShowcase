@echo off
call "%~dp0vision\run_vision.cmd" --preview %*
exit /b %errorlevel%
