using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(17)]
    public class Migration_017 : Migration
    {
        public override void Up()
        {
            Create.Column("AutoRefresh").OnTable("Tables").AsBoolean().WithDefaultValue(true);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}