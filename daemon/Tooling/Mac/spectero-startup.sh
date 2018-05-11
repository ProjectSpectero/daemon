#!/usr/bin/env bash

$SPECTERO_INSTALL_LOCATION="{install_location}"
$SPECTERO_VERSION="{install_version}"

if [ "$(ps -ef | grep -v grep | grep deamon.dll | wc-l)" -le 0 ]; then
    dotnet $SPECTERO_INSTALL_LOCATION/$SPECTERO_VERSION/daemon/daemon.dll 
else
    echo "Spectero Daemon is already running.";
fi
exit 0;