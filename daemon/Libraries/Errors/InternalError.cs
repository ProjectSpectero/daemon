using System.Net;

namespace Spectero.daemon.Libraries.Errors
{
    public class InternalError : BaseError
    {
        public InternalError(string why = Core.Constants.Errors.SOMETHING_WENT_WRONG,
            HttpStatusCode code = HttpStatusCode.InternalServerError) : base((int) code, why)
        {
        }
    }
}