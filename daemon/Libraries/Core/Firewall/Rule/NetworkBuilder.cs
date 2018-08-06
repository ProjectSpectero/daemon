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
using ServiceStack;
using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Network Builder
    ///
    /// This class just serves the purpose to keep classes shorter with quick functions that can be called to do iterable-like objects.
    /// All functions are intended to be static.
    /// </summary>
    public class NetworkBuilder
    {
        /// <summary>
        /// Compile a template using the provided network rule.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="networkRule"></param>
        /// <param name="interfaceInformation"></param>
        /// <returns></returns>
        public static string BuildTemplate(string template, NetworkRule networkRule, InterfaceInformation interfaceInformation)
        {
            // Replace all data in the template.
            template = template.ReplaceAll("{type}", networkRule.Type.ToString().ToUpper());
            template = template.ReplaceAll("{network}", networkRule.Network);
            template = template.ReplaceAll("{interface-address}", interfaceInformation.Address);
            template = template.ReplaceAll("{interface-name}", interfaceInformation.Name);
            template = template.ReplaceAll("{protocol}", networkRule.Protocol.ToString().ToLower());

            // Return the modified template to the user.
            return template;
        }

        /// <summary>
        /// Build a BOG-Standard instance of ProcessOptions to save space.
        /// </summary>
        /// <param name="executable"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static ProcessOptions BuildProcessOptions(string executable)
        {
            return new ProcessOptions()
            {
                Executable = executable,
                InvokeAsSuperuser = true,
                AttachLogToConsole = false
            };
        }
    }
}