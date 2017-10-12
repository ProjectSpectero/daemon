using System.Data;
using ServiceStack.OrmLite;
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
            }
        }

        public void Down()
        {
        }
    }
}