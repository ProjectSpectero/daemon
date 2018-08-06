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
using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using ServiceStack.DataAnnotations;

namespace Spectero.daemon.Models
{
    public class BaseModel : IModel
    {
        [Index]
        [AutoIncrement]
        public long Id { get; set; }

        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime UpdatedDate { get; set; }

        public virtual bool Validate(out ImmutableArray<string> errors, CRUDOperation operation = CRUDOperation.Create)
        {
            throw new NotImplementedException();
        }

        public string FormatValidationError(string errorKey, string field, string data = null)
        {
            return errorKey + ":" + field + ":" + data;
        }
    }
}