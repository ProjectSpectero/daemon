@echo off

IF exist "C:/Program Files/dotnet/dotnet.exe" (
    set _DNRT=C:/Program Files/dotnet/dotnet.exe
)
IF exist "C:/Program Files (x86)/dotnet/dotnet.exe" (
    set _DNRT="C:/Program Files (x86)/dotnet/dotnet.exe
)

for %%I in ("%~dp0..\..\..\") do set "grandparent=%%~fI"
IF exist "%grandparent%dotnet\dotnet.exe" (
    set "_DNRT=%grandparent%dotnet\dotnet.exe
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
set _all=%*
call set _tail=%%_all:*%2=%%
set _tail=%2%_tail%
"%_DNRT%" %~dp0/../Spectero.daemon.CLI.dll %_tail%
exit /b

:dotnet
echo Dotnet Framework Path: %_DNRT%
exit /b
