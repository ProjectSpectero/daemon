using System;

namespace Spectero.daemon.Libraries.Core.Firewall
{
    public class FirewallExceptions
    {
        /// <summary>
        /// Exception that signals kthe the operating system doesn't currently have an implementation in the daemon to handle the respectives firewall.
        /// You should only throw this inside of the constructor
        /// </summary>
        /// <returns></returns>
        public static Exception UnsupportedOperatingSystemException()
        {
            return new Exception("A firewall mechanism is not handled for this operating system.");
        }

        /// <summary>
        /// Generic exception to inform the console that the application has provided the wrong ruleset to the wrong function.
        /// </summary>
        /// <returns></returns>
        public static Exception NetworkRuleMismatchException()
        {
            return new Exception("The NetworkRule object you provided to the function is incompatable.");
        }

        public static Exception UnhandledNetworkRuleException()
        {
            return new Exception("The application did not know how to handle this instance of NetworkRule");
        }
    }
}