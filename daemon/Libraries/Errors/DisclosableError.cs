namespace Spectero.daemon.Libraries.Errors
{
    public class DisclosableError : BaseError
    {
        public DisclosableError() : base(400, "Invalid Request")
        {
        }
    }
}