﻿/*
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
using System.Collections.Immutable;
using System.Net;

namespace Spectero.daemon.Libraries.Errors
{
    public class ValidationError : BaseError
    {
        public ImmutableArray<string> Errors { get; }
        
        public ValidationError(ImmutableArray<string> errors) : base((int) HttpStatusCode.UnprocessableEntity,
            Core.Constants.Errors.VALIDATION_FAILED)
        {
            this.Errors = errors;
        }

        public ValidationError() : base((int) HttpStatusCode.UnprocessableEntity,
            Core.Constants.Errors.VALIDATION_FAILED)
        {
            this.Errors = ImmutableArray<string>.Empty;
        }
    }
}