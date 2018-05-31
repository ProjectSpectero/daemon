using System.Collections.Generic;
using Spectero.daemon.Libraries.Core.Identity;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    public class OpenVPNUserConfig
    {
        public string Identity;
        public User User;
        public IEnumerable<OpenVPNListener> Listeners;
        public OpenVPNConfig BaseConfig;
    }
}