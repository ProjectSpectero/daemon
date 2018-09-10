/*
    Spectero Daemon - Daemon Component to the Spectero Solution
    Copyright (C)  2017 Spectero, Inc.

    Spectero Daemon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Spectero Daemon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://github.com/ProjectSpectero/daemon/blob/master/LICENSE>.
*/
using System.Net;

namespace Spectero.daemon.Libraries.Errors
{
    public class DisclosableError : BaseError
    {
        // Could it be our fault? No! It's the user who is wrong.
        // https://i.kym-cdn.com/photos/images/newsfeed/000/645/713/888.jpg
        public string key { get;  }
        
        public DisclosableError(string key = Core.Constants.Errors.SOMETHING_WENT_WRONG, string why = Core.Constants.Errors.MODEL_BINDING_FAILED, HttpStatusCode code = HttpStatusCode.BadRequest) : base((int) code, why)
        {
            this.key = key;
        }
    }
}