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
namespace Spectero.daemon.Libraries.Core.Firewall.Rule
{
    /// <summary>
    /// Structure that defined the type of rule that was created.
    /// This object serves no purpose other than to give reference information.
    /// </summary>
    public struct NetworkRule
    {
        // The type of the rule.
        public NetworkRuleType Type;
        
        // The type of protocol the rule should use.
        public NetworkRuleProtocol Protocol;
        
        // The network that the rule should use, can be  stylized as 1.1.1.1 or 1.1.1.1-2.2.2.2.
        public string Network;
        
        // Reference to the interface name, not the type.
        public string Interface;
    }
}