namespace Spectero.daemon.Libraries.Errors
{
    public class EInternalError : BaseError
    {
        public EInternalError() : base(500, "Internal Server Error")
        {
        }
    }
}