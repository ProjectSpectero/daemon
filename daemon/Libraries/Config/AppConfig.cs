using System.Collections.Generic;

namespace Spectero.daemon.Libraries.Config
{
    public class AppConfig
    {
        public string Key { get; set; }
        public string BlockedRedirectUri { get; set; }
        public string DatabaseFile { get; set; }
        public double AuthCacheMinutes { get; set; }
        public bool LocalSubnetBanEnabled { get; set; }
        public Dictionary<string, Dictionary<string, string>> Defaults { get; set; }
    }
}