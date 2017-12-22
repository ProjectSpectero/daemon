namespace Spectero.daemon.Libraries.Core.Constants
{
    public class ConfigKeys
    {
        public const string SystemIdentity = "sys.id";
        public const string HttpConfig = "http.config";
        public const string PasswordHashingCost = "auth.password.cost";

        public const string CertificationAuthority = "crypto.ca.blob";
        public const string CeritificationAuthorityPassword = "crypto.ca.password";

        public const string ServerCertificate = "crypto.server.blob";
        public const string ServerCertificatePassword = "crypto.server.password";
        public const string ServerPFXChain = "crypto.server.chain";

        //OpenVPN
        public const string OpenVPNBaseConfig = "vpn.openvpn.config.template";
        public const string OpenVPNListeners = "vpn.openvpn.config.listeners";

        //JWT
        public const string JWTSymmetricSecurityKey = "crypto.jwt.key";

    }
}