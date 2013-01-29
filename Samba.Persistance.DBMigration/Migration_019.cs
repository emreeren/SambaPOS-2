using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(19)]
    public class Migration_019 : Migration
    {
        public override void Up()
        {
            Create.Column("ReplacementPattern").OnTable("Printers").AsString(128).Nullable();
        }

        public override void Down()
        {
            //do nothing
        }
    }
}