namespace Spectero.daemon.Libraries.Core
{
    public interface IAuthenticator
    {
        bool Authenticate(string username, string password);
    }
}