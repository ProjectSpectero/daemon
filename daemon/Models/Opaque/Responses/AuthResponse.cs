using Spectero.daemon.Models.Opaque;

namespace Spectero.daemon.Models.Responses
{
    public class AuthResponse
    {
        public Token Access;
        public Token Refresh;
    }
}