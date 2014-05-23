set installUtil = "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
@echo off
cls
cd %installUtil%
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /u C:\FalconSoft\ReactiveWorksheets\Src\Server\Server.ConsoleRunner\bin\Debug\ReactiveWorksheets.Server.ConsoleRunner.exe
pause