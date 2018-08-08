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
            throw new System.NotImplementedException();
        }

        public bool Delete(string symlink)
        {
            throw new System.NotImplementedException();
        }
    }
}