namespace Spectero.daemon.Libraries.Core.Constants
{
    public static class Errors
    {
        public const string VALIDATION_FAILED = "VALIDATION_FAILED";
        public const string OBJECT_PERSIST_FAILED = "OBJECT_PERSIST_FAILED";

        public const string FIELD_REQUIRED = "FIELD_REQUIRED";
        public const string FIELD_MAXLENGTH = "FIELD_MAXLENGTH";
        public const string FIELD_MINLENGTH = "FIELD_MINLENGTH";
        public const string FIELD_REGEX_MATCH = "FIELD_REGEX_MATCH";
        public const string FIELD_EMAIL = "FIELD_EMAIL";


        public const string MISSING_BODY = "MISSING_BODY";
        public const string USER_NOT_FOUND = "USER_NOT_FOUND";
        public const string RESOURCE_EXISTS = "RESOURCE_EXISTS";
        public const string AUTHENTICATION_FAILED = "AUTHENTICATION_FAILED";
        public const string ROLE_VALIDATION_FAILED = "ROLE_VALIDATION_FAILED";

        public const string ROLE_ESCALATION_FAILED = "ROLE_ESCALATION_FAILED";
        public const string CLOUD_USER_ALTER_NOT_ALLOWED = "CLOUD_USER_ALTER_NOT_ALLOWED";
        public const string USER_CANNOT_REMOVE_SELF = "USER_CANNOT_REMOVE_SELF";
        public const string CRUD_OPERATION_FAILED = "CRUD_OPERATION_FAILED";

        public const string INVALID_SERVICE_OR_ACTION_ATTEMPT = "INVALID_SERVICE_OR_ACTION_ATTEMPT";
        public const string INVALID_IP_AS_LISTENER_REQUEST = "INVALID_IP_AS_LISTENER_REQUEST";
        public const string INVALID_HTTP_MODE_REQUEST = "INVALID_HTTP_MODE_REQUEST";
        public const string STORED_CONFIG_WAS_NULL = "STORED_CONFIG_WAS_NULL";
        public const string AT_LEAST_ONE_SUPERADMIN_MUST_REMAIN = "AT_LEAST_ONE_SUPERADMIN_MUST_REMAIN";

        // CLoud Connect
        public const string CLOUD_ALREADY_CONNECTED = "CLOUD_ALREADY_CONNECTED";
    }
}