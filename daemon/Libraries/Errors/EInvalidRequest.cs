namespace Spectero.daemon.Libraries.Errors
{
    public class EInvalidRequest : BaseError
    {
        public EInvalidRequest() : base(400, "Invalid Request")
        {
            
        }
    }
}