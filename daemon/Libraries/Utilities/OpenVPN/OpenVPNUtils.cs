using System;
using System.IO;
using System.Linq;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Errors;

namespace Spectero.daemon.Utilities.OpenVPN
{
    public class OpenVPNUtils
    {
        /// <summary>
        /// Determine the absolute path of OpenVPN.
        /// </summary>
        /// <returns></returns>
        public static string DetermineBinaryPath()
        {
            // Placeholder variable to store the path.
            string binaryPath = null;

            // Check Linux/OS X Operating system for OpenVPN Installation.
            if (AppConfig.isUnix)
            {
                // Attempt to properly find the path of OpenVPN
                try
                {
                    var whichArray = new[] {"which", "openvpn"};
                    var whichFinder = Medallion.Shell.Command.Run("sudo", whichArray);

                    // Parse the output and get the absolute path.
                    var ovpnPath = whichFinder.StandardOutput.GetLines().ToList()[0];
                    
                    binaryPath = ovpnPath;
                }
                catch (Exception)
                {
                    // OpenVPN wasn't found.
                }
            }
            // Windows - Check Program Files Installations.
            else if (AppConfig.isWindows)
            {
                // Potential installation paths of OpenVPN.
                string[] potentialOpenVpnPaths =
                {
                    "C:\\Program Files (x86)\\OpenVPN\\bin\\openvpn.exe",
                    "C:\\Program Files\\OpenVPN\\bin\\openvpn.exe",
                };

                // Iterate through each potential path and find what exists.
                foreach (var currentOpenVpnPath in potentialOpenVpnPaths)
                {
                    if (!File.Exists(currentOpenVpnPath)) continue;
                   
                    binaryPath = currentOpenVpnPath;
                    break;
                }
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "OpenVPN: This daemon does not know how to initialize OpenVPN on this platform."
                );
            }
            
            // Return the found path.
            return binaryPath;
        }

       
    }
}