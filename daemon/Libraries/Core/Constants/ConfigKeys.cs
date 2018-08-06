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

        //CloudConnect
        public const string CloudConnectStatus = "cloud.connect.status";
        public const string CloudConnectIdentifier = "cloud.connect.id";
        public const string CloudConnectNodeKey = "cloud.connect.node-key";
        
        // Core Schema
        public const string SchemaVersion = "schema.version";

    }
}