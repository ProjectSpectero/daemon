namespace Spectero.daemon.Libraries.Core.Constants
{
    public class ConfigKeys
    {
        public const string SystemIdentity = "sys.id";
        public const string HttpListener = "http.listener";
        public const string HttpMode = "http.mode";
        public const string HttpAllowedDomains = "http.domains.allowed";
        public const string HttpBannedDomains = "http.domains.banned";
        public const string PasswordHashingCost = "auth.password.cost";
        public const string CertificationAuthority = "crypto.ca.blob";
        public const string CeritificationAuthorityPassword = "crypto.ca.password";
        public const string ServerCertificate = "crypto.server.blob";
        public const string ServerCertificatePassword = "crypto.server.password";
    }
}