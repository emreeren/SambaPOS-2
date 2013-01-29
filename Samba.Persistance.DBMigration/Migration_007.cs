using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(7)]
    public class Migration_007 : Migration
    {
        public override void Up()
        {
            Create.Column("MergeLines").OnTable("PrinterTemplates").AsBoolean().WithDefaultValue(false);
            Execute.Sql("Update PrinterTemplates set MergeLines=1");
        }

        public override void Down()
        {
            //do nothing
        }

    }
}
