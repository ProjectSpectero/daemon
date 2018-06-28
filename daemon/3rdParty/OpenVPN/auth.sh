#!/usr/bin/env bash

# Read the provided input.
readarray -t lines < $1;

# Sort values into variables.
username=${lines[0]};
password=${lines[1]};

# Developer sanity check.
if [ ! -f ../../../cli/bin/Debug/netcoreapp2.1/Spectero.daemon.CLI.dll ]; then
    # The daemon is likely running this, run it as expected.
    spectero cli auth OpenVPN $username $password;
else
    # The user is likely a developer, run the relative build.
    dotnet ../../../cli/bin/Debug/netcoreapp2.1/Spectero.daemon.CLI.dll auth OpenVPN $username $password;
fi

exit $?;