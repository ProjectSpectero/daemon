using System;

namespace Spectero.daemon.Libraries.Errors
{
    public class BaseError : Exception
    {
        public int Code { get; set; }

        protected BaseError(int code, string message) : base(message)
        {
            this.Code = code;
        }
    }
}