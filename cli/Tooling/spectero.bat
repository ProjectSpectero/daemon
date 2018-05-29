@echo off

IF exist "%~dp0/../../dotnet/dotnet.exe" (
    set _DNRT=%~dp0/../../dotnet/dotnet.exe
)
IF exist "C:/Program Files/dotnet/dotnet.exe" (
    set _DNRT=C:/Program Files/dotnet/dotnet.exe
)
IF exist "C:/Program Files (x86)/dotnet/dotnet.exe" (
    set _DNRT="C:/Program Files (x86)/dotnet/dotnet.exe
)

IF "%_DNRT%"=="" goto error_dotnet

IF "%~1"=="cli" goto cli
IF "%~1"=="dotnet" goto dotnet
goto error_noarg


:error_noarg
echo No arguments specified
echo Available commands: 'cli', 'dotnet'
exit /b

:error_dotnet
echo Dotnet Core 2.0 Framework is not installed.
exit /b

:cli
"%_DNRT%" %~dp0/../Spectero.daemon.CLI.dll %2
exit /b

:dotnet
echo Dotnet Framework Path: %_DNRT%
exit /b
