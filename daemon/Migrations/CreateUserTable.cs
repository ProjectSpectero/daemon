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

using System;
using System.Collections.Generic;
using FluentMigrator;
using Spectero.daemon.Models;

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