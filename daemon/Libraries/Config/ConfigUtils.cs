using System.Data;
using System.Threading.Tasks;
using ServiceStack.OrmLite;
using Spectero.daemon.Models;

namespace Spectero.daemon.Libraries.Config
{
    public static class ConfigUtils
    {
        public static async Task<Configuration> CreateOrUpdateConfig(IDbConnection db, string key, string value)
        {
            var tmp =
                await db.SingleAsync<Configuration>(x => x.Key == key) ??
                new Configuration();

            tmp.Value = value;

            if (tmp.Id != 0L)
                await db.UpdateAsync(tmp);
            else
            {
                tmp.Key = key;
                await db.InsertAsync(tmp);
            }


            return tmp;
        }

        public static async Task<Configuration> GetConfig(IDbConnection db, string key)
        {
            return await db.SingleAsync<Configuration>(x => x.Key == key);
        }

        public static async Task<int> DeleteConfigIfExists(IDbConnection db, string key)
        {
            return await db.DeleteAsync<Configuration>(x => x.Key == key);
        }
    }
}