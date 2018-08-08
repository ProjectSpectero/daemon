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