using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    /*
     * This class support employee scheding table
     * Disable multiple item selections from item list view
     * Creating column for early checkin time
     */
    [Migration(22)]
    class Migration_022 : Migration
	{
        public override void Up()
        {
            if (!Schema.Table("EmpScheduleEntries").Exists())
            {

                Create.Table("EmpScheduleEntries")
                      .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                      .WithColumn("UserId").AsInt32().WithDefaultValue(0)
                      .WithColumn("Name").AsString(128).Nullable()
                      .WithColumn("StartTime").AsDateTime().WithDefaultValue(DateTime.Today.AddHours(18))
                      .WithColumn("EndTime").AsDateTime().WithDefaultValue(DateTime.Today.AddHours(23));
            }
            if (!Schema.Table("Users").Column("Wages").Exists())
            {
                Create.Column("Wages").OnTable("Users").AsDecimal().Nullable();
            }
            if (!Schema.Table("Terminals").Column("DisableMultipleItemSelection").Exists())
            {
                Create.Column("DisableMultipleItemSelection").OnTable("Terminals").AsBoolean().WithDefaultValue(false);
            }
            if (!Schema.Table("Terminals").Column("EarlyClockInAllowedInMinutes").Exists())
            {
                Create.Column("EarlyClockInAllowedInMinutes").OnTable("Terminals").AsBoolean().WithDefaultValue(5);
            }

        }
        public override void Down()
        {
            //do nothing
        }

	}
}
