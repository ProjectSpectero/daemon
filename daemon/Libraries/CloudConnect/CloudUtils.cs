using System.Data;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.CloudConnect
{
    public static class CloudUtils
    {
        // TODO: Test this thoroughly
        public static async Task<bool> IsConnected (IDbConnection Db)
        {
            var storedConfig = await Db
                .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectStatus);

            if (!bool.TryParse(storedConfig?.Value, out var result) || !result) return false;
            {
                var nodeIdConfig = await Db
                    .SingleAsync<Configuration>(x => x.Key == ConfigKeys.CloudConnectIdentifier);

                return nodeIdConfig != null;
            }

        }
    }
}