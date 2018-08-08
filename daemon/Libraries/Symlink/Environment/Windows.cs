using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

namespace Spectero.daemon.Libraries.Symlink
{
    public class Windows : ISymlinkEnvironment
    {
        private Symlink _parent;

        public Windows(Symlink parent)
        {
            _parent = parent;
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, Symlink.SymbolicLink dwFlags);

        public bool Create(string symlink, string absolutePath)
        {
            return CreateSymbolicLink(symlink, absolutePath, _parent.GetAbsolutePathType(absolutePath));
        }

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