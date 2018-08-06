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
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public class AuthUtils
    {
        public static long GenerateViableCost(string testPassword, int iterations = 50, double timeTarget = 100,
            int costThreshold = 10, int cost = 16)
        {
            long timeTaken;
            do
            {
                var sw = Stopwatch.StartNew();
                BCrypt.Net.BCrypt.HashPassword(testPassword, cost);

                sw.Stop();
                timeTaken = sw.ElapsedMilliseconds;

                cost -= 1;
            } while (timeTaken >= timeTarget && iterations > 0);

            if (cost + 1 < costThreshold)
                return costThreshold;
            return cost + 1;
        }

        public static string GetCachedUserPasswordKey(string username)
        {
            return "auth.user.inmemory." + username;
        }

        public static string GetCachedUserKey(string username)
        {
            return "auth.user." + username;
        }

        public static void ClearUserFromCacheIfExists(IMemoryCache cache, string authKey)
        {
            var key = GetCachedUserKey(authKey);
            var passwordKey = GetCachedUserPasswordKey(authKey);
            
            cache.Remove(key);
            cache.Remove(passwordKey);
        }
    }
}