using Spectero.daemon.Libraries.Core.ProcessRunner;

namespace Spectero.daemon.Libraries.Symlink
{
    public interface ISymlink
    {
        // Determination
        Symlink.SymbolicLink GetAbsolutePathType(string absolutePath);
        bool IsSymlink(string linkPath);
        
        // Symlink Environment
        ISymlinkEnvironment GetEnvironment();
        
        // Process Runner
        void SetProcessRunner(IProcessRunner processRunner);
        IProcessRunner GetProcessRunner();
    }
}