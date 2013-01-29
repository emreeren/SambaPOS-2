using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(13)]
    public class Migration_013 : Migration
    {
        public override void Up()
        {
            Create.Column("GroupTemplate").OnTable("PrinterTemplates").AsString(500).Nullable();
            Create.Column("CloseTicket").OnTable("PrintJobs").AsBoolean().WithDefaultValue(true);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
