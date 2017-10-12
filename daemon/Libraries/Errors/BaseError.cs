using System;

namespace Spectero.daemon.Libraries.Errors
{
    public class BaseError : Exception
    {
        public BaseError(int code, string message)
        {
        }

        internal int code { get; set; }

        public int getCode()
        {
            return code;
        }
    }
}