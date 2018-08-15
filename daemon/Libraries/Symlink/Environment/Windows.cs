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
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Medallion.Shell;
using ServiceStack;
using Spectero.daemon.Libraries.Config;
using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Symlink
{
    /// <summary>
    /// Environment class for Windows.
    /// Specifically here we can use kernel calls to manage the filesystem and it's symbolic links.
    /// We do so here by carefully determining what the target path is, and having conditional
    /// statements to make sure we safely handle what we want.
    /// </summary>
    public class Windows : ISymlinkEnvironment
    {
        /// <summary>
        /// The parent class is the class that will initialize this environment.
        /// </summary>
        private Symlink _parent;

        /// <summary>
        /// Constructor
        /// (Inherits the parent class)
        /// </summary>
        /// <param name="parent"></param>
        public Windows(Symlink parent)
        {
            _parent = parent;
        }

        [DllImport("kernel32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, Symlink.SymbolicLink dwFlags);

        /// <summary>
        /// Wrapper function to create symlink and determine the type from the absolute path.
        /// 0 = file
        /// 1 = directory
        /// </summary>
        /// <param name="symlink"></param>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public bool Create(string symlink, string absolutePath)
        {
            return CreateSymbolicLink(symlink, absolutePath, _parent.GetAbsolutePathType(absolutePath));
        }

        /// <summary>
        /// Delete the symlink safely.
        /// </summary>
        /// <param name="linkPath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool Delete(string linkPath)
        {
            if (_parent.IsSymlink(linkPath))
            {
                switch (_parent.GetAbsolutePathType(linkPath))
                {
                    case Symlink.SymbolicLink.Directory:
                        Directory.Delete(linkPath);
                        return true;

                    case Symlink.SymbolicLink.File:
                        File.Delete(linkPath);
                        return true;

                    default:
                        return false;
                }
            }
            else
            {
                throw new Exception("Specified deletion path is not a symbolic link.");
            }
        }
    }
}