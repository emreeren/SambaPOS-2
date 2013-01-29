using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(11)]
    public class Migration_011 : Migration
    {
        public override void Up()
        {
            Create.Column("GroupCode").OnTable("Customers").AsString(128).Nullable();
            Create.Column("CustomerGroupCode").OnTable("Tickets").AsString(128).Nullable();
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
