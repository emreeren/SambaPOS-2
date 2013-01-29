using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(16)]
    public class Migration_016 : Migration
    {
        public override void Up()
        {
            Create.Column("ExcludeInReports").OnTable("TicketTagGroups").AsBoolean().WithDefaultValue(false);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}