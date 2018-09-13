namespace Spectero.daemon.Libraries.Utilities.Architecture
{
    public interface IArchitectureUtility
    {
        // Dynamic environment reader to read one of the below.
        string GetArchitecture();
        
        // Operating system specific.
        string GetWindowsArchitecture();
        string GetLinuxArchitecture();
        string GetUnixArchitecture();
    }
}