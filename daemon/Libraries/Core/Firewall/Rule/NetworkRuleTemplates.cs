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
    /// For each type of firewall, please define it in here for organization's sake.
    /// </summary>
    public class NetworkRuleTemplates
    {
        public const string SNAT = "-t nat POSTROUTING -o {interface-name} -J SNAT --to {interface-address}";
        public const string MASQUERADE = "POSTROUTING -s {network} -o {interface-name} -J MASQUERADE";
    }
}