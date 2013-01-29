using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(15)]
    public class Migration_015 : Migration
    {
        public override void Up()
        {
            Create.Column("HtmlContent").OnTable("Tables").AsString(128).Nullable();
            Create.Column("ForceValue").OnTable("MenuItemPropertyGroups").AsBoolean().WithDefaultValue(false);
            Create.Column("GroupTag").OnTable("MenuItemPropertyGroups").AsString(128).Nullable();
        }

        public override void Down()
        {
            //do nothing
        }
    }
}