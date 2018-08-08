namespace Spectero.daemon.Libraries.Symlink
{
    public interface ISymlinkEnvironment
    {
        bool Create(string symlink, string absolutePath);
        bool Delete(string symlink);
    }
}