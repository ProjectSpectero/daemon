using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Spectero.daemon.Libraries.Config
{
    public class AppConfig
    {
        public string BlockedRedirectUri { get; set; }
        public string DatabaseFile { get; set; }
        public double AuthCacheMinutes { get; set; }
        public bool LocalSubnetBanEnabled { get; set; }
        public Dictionary<string, Dictionary<string, string>> Defaults { get; set; }
        public int PasswordCostLowerThreshold { get; set; }
        public int JWTTokenExpiryInMinutes { get; set; }
        public int PasswordCostCalculationIterations { get; set; }
        public string PasswordCostCalculationTestTarget { get; set; }
        public double PasswordCostTimeThreshold { get; set; }

        public static bool isWindows => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool isLinux => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool isMac => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool isUnix => System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public bool RespectEndpointToOutgoingMapping { get; set; }
        public bool BindToUnbound { get; set; }
        public string WebRoot { get; set; }
        public bool SpaMode { get; set; }
        public string LoggingConfig { get; set; }
        public string SpaFileName { get; set; }
        public int SpaCacheTime { get; set; }
        public string DefaultOutgoingIPResolver { get; set; }

        public bool InMemoryAuth { get; set; }
        public int InMemoryAuthCacheMinutes { get; set; }
        public bool AutoStartServices { get; set; }
        public bool LogCommonProxyEngineErrors { get; set; }
        public bool IgnoreRFC1918 { get; set; }
        public string JobsConnectionString { get; set; }

        public static string ApiBaseUri
        {
            // Mostly hardcoded, a discovery service is not planned for the MVP
            get
            {
                switch (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLower())
                {
                    case "development":
                        return $"https://dev.spectero.com/v1/";
                    case "local":
                        return $"http://homestead.marketplace/v1/";
                    default:
                        return $"https://api.spectero.com/v1/";
                }
            }
        }

        public static string CloudConnectDefaultAuthKey => "cloud";
    }
}