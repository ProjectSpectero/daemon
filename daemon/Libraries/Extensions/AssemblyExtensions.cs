using System;
using System.IO;
using System.Reflection;

namespace Spectero.daemon.Libraries.Extensions
{
    public static class AssemblyExtensions
    {
        public static string GetDirectoryPath(this Assembly assembly)
        {
            var filePath = new Uri(assembly.CodeBase).LocalPath;
            return Path.GetDirectoryName(filePath);            
        }
    }
}