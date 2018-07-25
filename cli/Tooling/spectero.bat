@echo off

REM [CHECK IF DOTNET IS INSTALLED]
WHERE dotnet >nul 2>nul
IF %ERRORLEVEL% NEQ 0 goto error_dotnet

REM [PARSE SECOND ARGUMENT]
IF "%~1"=="cli" goto cli
IF "%~1"=="dotnet" goto dotnet

REM [HANDLE NO ARGUMENT]
goto error_noarg


REM [FUNCTION TO TELL USER THEY NEED A SECONDARY ARGUMENT]
:error_noarg
echo No arguments specified
echo Available commands: 'cli', 'dotnet'
exit /b


REM [FUNCTION TO TELL USER THAT THEY DIDNT INSTALL DOTNET CORE]
:error_dotnet
echo Dotnet Core 2.0 Framework is not installed.
exit /b


REM [FUNCTION TO OPEN CLI INTERFACE]
:cli
set _all=%*
call set _tail=%%_all:*%2=%%
set _tail=%2%_tail%
dotnet "%~dp0..\Spectero.daemon.CLI.dll" %_tail%
exit /b


REM [FUNCTION TO CHECK DOTNET CORE PATHS]
:dotnet
echo Dotnet Framework Paths:
WHERE dotnet
exit /b

