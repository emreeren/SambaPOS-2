using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(10)]
    public class Migration_010 : Migration
    {
        public override void Up()
        {
            Create.Column("PriceTags").OnTable("TicketTagGroups").AsBoolean().WithDefaultValue(false);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
