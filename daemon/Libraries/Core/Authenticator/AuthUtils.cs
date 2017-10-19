using System.Diagnostics;

namespace Spectero.daemon.Libraries.Core.Authenticator
{
    public class AuthUtils
    {
        public static long GenerateViableCost(string testPassword, int iterations = 50, double timeTarget = 100, int costThreshold = 10, int cost = 16)
        {
            long timeTaken;
            do
            {
                var sw = Stopwatch.StartNew();
                BCrypt.Net.BCrypt.HashPassword(testPassword, workFactor: cost);

                sw.Stop();
                timeTaken = sw.ElapsedMilliseconds;

                cost -= 1;

            } while ((timeTaken) >= timeTarget && iterations > 0);

            if ((cost + 1) < costThreshold)
                return costThreshold;
            else
                return cost + 1;
        }
    }
}