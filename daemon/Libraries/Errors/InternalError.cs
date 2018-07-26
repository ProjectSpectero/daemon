namespace Spectero.daemon.Libraries.Errors
{
    public class InternalError : BaseError
    {
        public InternalError(string why = Core.Constants.Errors.SOMETHING_WENT_WRONG) : base(500, why)
        {
        }
    }
}