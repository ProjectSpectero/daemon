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
using Medallion.Shell;
using Spectero.daemon.Libraries.Core.ProcessRunner;
using Spectero.daemon.Libraries.Errors;

namespace Spectero.daemon.Libraries.Symlink
{
    public class Unix : ISymlinkEnvironment
    {
        private Symlink _parent;

        public Unix(Symlink parent)
        {
            _parent = parent;
        }

        public bool Create(string symlink, string absolutePath)
        {
            var procOptions = new ProcessOptions
            {
                Executable = "ln",
                Arguments = new[] {"-s", absolutePath, symlink},
                Monitor = false,
                DisposeOnExit = true,
            };

            try
            {
                _parent.processRunner.Run(procOptions).Command.Wait();
                return true;
            }
            catch (ErrorExitCodeException exception)
            {
                throw new Exception("A error occured while attempting to create a unix symbolic link.\n" + exception);
            }
        }

        public bool Delete(string symlink)
        {
            var procOptions = new ProcessOptions
            {
                Executable = "unlink",
                Arguments = new[] {symlink},
                Monitor = false,
                DisposeOnExit = true,
            };

            try
            {
                _parent.processRunner.Run(procOptions).Command.Wait();
                return true;    
            }
            catch (ErrorExitCodeException exception)
            {
                throw new Exception("A error occured while attempting to delete a unix symbolic link.\n" + exception);
            }
        }
    }
}