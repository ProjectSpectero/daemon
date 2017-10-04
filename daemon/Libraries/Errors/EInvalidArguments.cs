namespace Spectero.daemon.Libraries.Errors
{
    public class EInvalidArguments : BaseError
    {
        public EInvalidArguments() : base(500, "Invalid arguments provided")
        {
            
        }
    }
}