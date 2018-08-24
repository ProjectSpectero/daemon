using FluentMigrator;

namespace Spectero.daemon.Migrations
{
    [Migration(20180822203600)]
    public class CreateUserTable : Migration
    {
        public override void Up()
        {
            Create.Table("User")
                .WithColumn("Id").AsInt32().PrimaryKey().Nullable()
                .WithColumn("Source").AsInt32()
                .WithColumn("AuthKey").AsString().Indexed("uidx_user_authkey")
                .WithColumn("Roles").AsString().Nullable()
                .WithColumn("Password").AsString()
                .WithColumn("Cert").AsString().Nullable()
                .WithColumn("CertKey").AsString().Nullable()
                .WithColumn("EngagementId").AsInt64()
                .WithColumn("FullName").AsString().Nullable()
                .WithColumn("EmailAddress").AsString().Nullable()
                .WithColumn("LastLoginDate").AsString()
                .WithColumn("CloudSyncDate").AsString()
                .WithColumn("CreatedDate").AsString()
                .WithColumn("UpdatedDate").AsString();
        }

        public override void Down()
        {
            Delete.Table("User");
        }
    }
}