using FluentMigrator;

namespace Spectero.daemon.Migrations
{
    [Migration(20180822203602)]
    public class CreateConfigurationTable : Migration
    {
        public override void Up()
        {
            Create.Table("Configuration")
                .WithColumn("Id").AsInt32().PrimaryKey().Nullable()
                .WithColumn("Key").AsString().Nullable()
                .WithColumn("Value").AsString().Nullable()
                .WithColumn("CreatedDate").AsString()
                .WithColumn("UpdatedDate").AsString();
        }

        public override void Down()
        {
            Delete.Table("Configuration");
        }
    }
}