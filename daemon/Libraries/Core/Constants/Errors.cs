namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Errors
    {
        public const string MISSING_BODY = "MISSING_BODY";
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string MISSING_OR_INVALID_PASSWORD = "MISSING_OR_INVALID_PASSWORD";
        public const string AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED";
        public const string ROLE_VALIDATION_FAILED = "ROLE_VALIDATION_FAILED";
        public const string ROLE_ESCALATION_FAILED = "ROLE_ESCALATION_FAILED";
        public const string CLOUD_USER_ALTER_NOT_ALLOWED = "CLOUD_USER_ALTER_NOT_ALLOWED";
        public const string USER_CANNOT_REMOVE_SELF = "USER_CANNOT_REMOVE_SELF";
        public const string INVALID_IP_AS_LISTENER_REQUEST = "INVALID_IP_AS_LISTENER_REQUEST";
        public const string INVALID_HTTP_MODE_REQUEST = "INVALID_HTTP_MODE_REQUEST";
        public const string STORED_CONFIG_WAS_NULL = "STORED_CONFIG_WAS_NULL";
    }
}