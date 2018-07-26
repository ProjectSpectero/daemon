using System.Net;

namespace Spectero.daemon.Libraries.Errors
{
    public class DisclosableError : BaseError
    {
        // Could it be our fault? No! It's the user who is wrong.
        // https://i.kym-cdn.com/photos/images/newsfeed/000/645/713/888.jpg
        public string key { get;  }
        
        public DisclosableError(string key = Core.Constants.Errors.SOMETHING_WENT_WRONG, string why = Core.Constants.Errors.U_WOT_M8, HttpStatusCode code = HttpStatusCode.BadRequest) : base((int) code, why)
        {
            this.key = key;
        }
    }
}