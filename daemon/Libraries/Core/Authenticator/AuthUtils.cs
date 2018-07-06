using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Spectero.daemon.Models;

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