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
using System.Collections.Generic;

namespace Spectero.daemon.Libraries.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> Chunk(this string str, int chunkSize)
        {
            if(string.IsNullOrEmpty(str) || chunkSize<1)
                throw new ArgumentException("String can not be null or empty and chunk size should be greater than zero.");
            
            var chunkCount = str.Length / chunkSize + (str.Length % chunkSize != 0 ? 1 : 0);
            for (var i = 0; i < chunkCount; i++)
            {
                var startIndex = i * chunkSize;
                if (startIndex + chunkSize >= str.Length)
                    yield return str.Substring(startIndex);
                else
                    yield return str.Substring(startIndex, chunkSize);
            }
        }
    }
}