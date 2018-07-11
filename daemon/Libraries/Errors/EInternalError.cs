namespace Spectero.daemon.Libraries.Errors
{
    public class EInternalError : BaseError
    {
        public EInternalError(string why = "Internal Server Error") : base(500, why)
        {
        }
    }
}