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

namespace Spectero.daemon.Libraries.Core.ProcessRunner
{
    public interface IProcessRunner
    {
        // The first one contains all details about how to run and configure the 3rd party binary, the 2nd one is for monitoring/such synchronization only.
        CommandHolder Run(ProcessOptions processOptions, IProcessTrackable caller = null);

        void CloseAllTrackedCommands();
        void CloseAllBelongingToService(IProcessTrackable service, bool force = false);
        void TerminateAllTrackedCommands();
        void RestartAllTrackedCommands(bool force);
    }
}