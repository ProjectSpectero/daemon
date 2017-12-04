using System.Collections.Generic;
using System.Runtime.InteropServices;

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
    }
}