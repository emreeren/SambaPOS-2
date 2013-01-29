using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(3)]
    public class Migration_003 : Migration
    {
        public override void Up()
        {
            Create.Table("Permissions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString().WithDefaultValue("").Nullable()
                .WithColumn("UserRole_Id").AsInt32().Nullable()
                .WithColumn("Value").AsInt32().WithDefaultValue(0);

            Create.ForeignKey("UserRole_Permissions")
                .FromTable("Permissions").ForeignColumn("UserRole_Id")
                .ToTable("UserRoles").PrimaryColumn("Id");

             Delete.Column("CanExecuteDashboard").FromTable("UserRoles");
        }

        public override void Down()
        {
            Delete.Table("Permissions");
        }
    }
}
