#!/usr/bin/env bash

readarray -t lines < $1;
username=${lines[0]};
password=${lines[1]};

spectero cli auth OpenVPN $username $password;

exit -e $?;