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
using System.IO;
using Spectero.daemon.Libraries.Config;

namespace Spectero.daemon.Libraries.Symlink
{
    public class Symlink
    {
        private ISymlinkEnvironment _environment;

        public enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        public Symlink()
        {
            if (AppConfig.isWindows) _environment = new Windows(this);
            if (AppConfig.isUnix) _environment = new Unix(this);
        }

        public SymbolicLink GetAbsolutePathType(string absolutePath)
        {
            // Check if file.
            if (File.Exists(absolutePath))
                return SymbolicLink.File;

            // Check if directory
            if (Directory.Exists(absolutePath))
                return SymbolicLink.Directory;

            // Unsure, we should never reach this point but let's decide to use a file.
            return SymbolicLink.File;
        }

        public bool IsSymlink(string linkPath)
        {
            FileInfo pathInfo = new FileInfo(linkPath);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        public ISymlinkEnvironment Environment => _environment;
    }