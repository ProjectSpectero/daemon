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