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
using System.Net;
using Spectero.daemon.Libraries.Core;
using Spectero.daemon.Libraries.Extensions;
using Valit;

namespace Spectero.daemon.Libraries.Services.OpenVPN.Elements
{
    public class OpenVPNListener
    {
        // This *should* be modelled as an IPAddress, but that just incurs additional serialization/deserialization overhead.
        public string IPAddress;

        // The Nullable<T> wrapping is so Valit works with it, since this is a "value type" as far as C# is concerned.
        // Quite annoying baggage wise, but it works.
        // This means that before usage, you need to cast it to their concrete versions (and also run the risk of dealing with InvalidOperationException)
        public int? Port;
        public int? ManagementPort;
        public TransportProtocol? Protocol;

        // This *should* be modelled as an IPNetwork, but that just incurs additional serialization/deserialization overhead.
        public string Network;

        public static IValitator<OpenVPNListener> validator
            => ValitRules<OpenVPNListener>
                .Create()
                .Ensure(m => m.IPAddress, _ => _
                    .Required()
                        .WithMessage("FIELD_REQUIRED: ipAddress")
                    .Satisfies(x => System.Net.IPAddress.TryParse(x, out var _))
                        .WithMessage($"{Core.Constants.Errors.FIELD_INVALID}:listeners.ipAddress"))
                .Ensure(m => m.Network, _ => _
                    .Satisfies(x => IPNetwork.TryParse(x, out var parsedNetwork) && parsedNetwork.FirstUsable.IsInternal())
                        .WithMessage($"{Core.Constants.Errors.FIELD_INVALID}:listeners.network"))
                .Ensure(m => m.Port, _ => _
                    .Required()
                        .WithMessage($"{Core.Constants.Errors.FIELD_REQUIRED}:listeners.port")
                    .IsGreaterThan(1023)
                        .WithMessage($"{Core.Constants.Errors.FIELD_MINLENGTH}:listeners.port:1024")
                    .IsLessThan(65535)
                        .WithMessage($"{Core.Constants.Errors.FIELD_MAXLENGTH}:listeners.port:65534"))
                .Ensure(m => m.ManagementPort, _ => _
                    .Required()
                        .WithMessage($"{Core.Constants.Errors.FIELD_REQUIRED}:listeners.ManagementPort")
                    .IsGreaterThan(1023)
                        .WithMessage($"{Core.Constants.Errors.FIELD_MINLENGTH}:listeners.ManagementPort:1024")
                    .IsLessThan(65535)
                        .WithMessage($"{Core.Constants.Errors.FIELD_MAXLENGTH}:listeners.ManagementPort:65534"))
                .Ensure(m => m.Protocol, _ => _
                    .Satisfies(x => x.HasValue)
                        .WithMessage($"{Core.Constants.Errors.FIELD_INVALID}:listeners.Protocol"))
                .CreateValitator();
    }
}