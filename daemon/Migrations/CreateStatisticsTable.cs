using FluentMigrator;

namespace Spectero.daemon.Migrations
{
    [Migration(20180822203601)]
    public class CreateStatisticsTable : Migration
    {
        public override void Up()
        {
            Create.Table("Statistic")
                .WithColumn("Id").AsInt32().PrimaryKey().Nullable()
                .WithColumn("Bytes").AsInt64()
                .WithColumn("Directions").AsString()
                .WithColumn("CreatedDate").AsString()
                .WithColumn("UpdatedDate").AsString();
        }

        public override void Down()
        {
            Delete.Table("Statistic");
        }
    }
}