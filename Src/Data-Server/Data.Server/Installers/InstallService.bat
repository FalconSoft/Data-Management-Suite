@setlocal enableextensions
@cd /d "%~dp0"
@echo off
cls
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe .\..\bin\Debug\FalconSoft.Data.Server.exe
net start DMSServerService
pause