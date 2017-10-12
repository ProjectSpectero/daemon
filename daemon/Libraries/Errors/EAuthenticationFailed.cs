namespace Spectero.daemon.Libraries.Errors
{
    public class EAuthenticationFailed : BaseError
    {
        public EAuthenticationFailed() : base(403, "Access denied")
        {
        }
    }
}