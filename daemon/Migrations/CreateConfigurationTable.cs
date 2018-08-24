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