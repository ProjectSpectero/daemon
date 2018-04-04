@echo off

where dotnet >nul 2>nul
IF %errorlevel%==1 (
    @echo Error: dotnet is not installed.
    exit /b
)

IF "%~1"=="cli" goto cli

echo No arguments specified
echo Available commands: 'cli'
exit /b


:cli
cd cli
dotnet Spectero.daemon.CLI.dll