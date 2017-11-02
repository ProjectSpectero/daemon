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
        public const string ServerPFXChain = "crypto.server.chain";

        //OpenVPN
        public const string OpenVPNAllowMultipleConnectionsFromSameClient = "vpn.openvpn.allow_multiple_connections";
        public const string OpenVPNAllowClientToClient = "vpn.openvpn.allow_client_to_client";
        public const string OpenVPNListeners = "vpn.openvpn.listeners";
        public const string OpenVPNDHCPOptions = "vpn.openvpn.dhcp_options";
        public const string OpenVPNPushedNetworks = "vpn.openvpn.pushed_networks";
        public const string OpenVPNMaxClients = "vpn.openvpn.max_clients";
        public const string OpenVPNRedirectGatewayOptions = "vpn.openvpn.redirect_gateway";

    }
}