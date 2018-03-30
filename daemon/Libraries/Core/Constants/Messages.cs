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
    }
}