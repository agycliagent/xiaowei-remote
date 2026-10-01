@echo off
chcp 65001 >nul
echo 正在停止小薇远程所有后台服务与隧道...
powershell -ExecutionPolicy Bypass -File "%~dp0vps_tunnel.ps1" -Action Stop
pause
