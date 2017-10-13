using System.Data;
using ServiceStack;
using ServiceStack.OrmLite;
using Spectero.daemon.Libraries.Core.Constants;
using Spectero.daemon.Libraries.Services.HTTPProxy;
using Spectero.daemon.Models;

namespace Spectero.daemon.Migrations
{
    public class Initialize : IMigration
    {
        private readonly IDbConnection _db;

        public Initialize(IDbConnection db)
        {
            _db = db;
        }

        public void Up()
        {
            if (!_db.TableExists<User>())
                _db.CreateTable<User>();

            if (!_db.TableExists<Statistic>())
                _db.CreateTable<Statistic>();

            if (!_db.TableExists<Configuration>())
            {
                _db.CreateTable<Configuration>();
                _db.Insert(new Configuration
                {
                    Key = "http.listener",
                    Value = Defaults.HTTP.ToJson()
                });
                _db.Insert(new Configuration
                {
                    Key = "http.mode",
                    Value = HTTPProxyModes.Normal.ToJson()
                });
                _db.Insert(new Configuration
                {
                    Key = "http.domains.allowed",
                    Value = ""
                });
                _db.Insert(new Configuration
                {
                    Key = "http.domains.banned",
                    Value = ""
                });
            }
        }

        public void Down()
        {
        }
    }
}