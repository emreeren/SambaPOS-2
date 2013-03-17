using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
	[Migration(21)]
	public class Migration_021 : Migration
	{
		public override void Up()
		{

            Create.Column("ButtonColor").OnTable("MenuItemPropertyGroups").AsString(128).Nullable();
            Execute.Sql("Update MenuItemPropertyGroups set ButtonColor='LightGray'");

		    Create.Table("TimeCardEntries")
		          .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                  .WithColumn("Name").AsString(128).Nullable()
		          .WithColumn("UserId").AsInt32().WithDefaultValue(0)
		          .WithColumn("Action").AsInt32().WithDefaultValue(0)
		          .WithColumn("DateTime").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

		    Create.Column("ContactPhone").OnTable("Users").AsString(128).Nullable();
		    Create.Column("EmergencyPhone").OnTable("Users").AsString(128).Nullable();
		    Create.Column("DateOfBirth").OnTable("Users").AsString(128).Nullable();
		}

		public override void Down()
		{
			//do nothing
		}
	}
}
