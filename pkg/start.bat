@echo off
chcp 65001 >nul
echo 正在启动小薇远程后台守护...
powershell -ExecutionPolicy Bypass -File "%~dp0vps_tunnel.ps1" -Action Start
pause
