/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Messages
    {
        // Static data warehouse of all the message keys the API might return
        public const string SERVICE_STARTED = "SERVICE_STARTED";
        public const string SERVICE_STOPPED = "SERVICE_STOPPED";
        public const string SERVICE_RESTARTED = "SERVICE_RESTARTED";

        public const string ACTION_FAILED = "ACTION_FAILED";

        public const string SERVICE_RESTART_NEEDED = "SERVICE_RESTART_NEEDED";
        public const string DAEMON_RESTART_NEEDED = "DAEMON_RESTART_NEEDED";

        public const string JWT_TOKEN_ISSUED = "JWT_TOKEN_ISSUED";
        public const string USER_AUTHKEY_FLATTENED = "USER_AUTHKEY_FLATTENED";

        public const string SPECTERO_USERNAME_PASSWORD = "SPECTERO_USERNAME_PASSWORD";

        public const string AUTHENTICATION_SUCCEEDED = "AUTHENTICATION_SUCCEEDED";
        public const string CLOUD_CONNECTED_SUCCESSFULLY = "CLOUD_CONNECTED_SUCCESSFULLY";

        public const string APPLICATION_STATE_TOGGLED = "APPLICATION_STATE_TOGGLED";
    }
}