@echo off
chcp 65001 >nul
powershell -ExecutionPolicy Bypass -File "%~dp0vps_tunnel.ps1" -Action Status
pause
