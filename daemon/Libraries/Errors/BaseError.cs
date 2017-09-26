using System;

namespace Spectero.daemon.Libraries.Errors
{
    public class BaseError : Exception
    {
        internal int code { get; set; }

        public BaseError (int code, string message)
        {
            
        }

        public int getCode()
        {
            return code;
        }
    }
}