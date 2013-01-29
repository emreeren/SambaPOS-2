using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(12)]
    public class Migration_012 : Migration
    {
        public override void Up()
        {
            Create.Column("IsImageOnly").OnTable("ScreenMenuItems").AsBoolean().WithDefaultValue(false);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
