using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(2)]
    public class Migration_002 : Migration
    {
        public override void Up()
        {
            Create.Column("TerminalTableScreenId").OnTable("Departments").AsInt32().WithDefaultValue(0);
            Create.Column("TableScreenId").OnTable("Departments").AsInt32().WithDefaultValue(0);
            Create.Column("ScreenMenuId").OnTable("Departments").AsInt32().WithDefaultValue(0);
            Create.Column("TerminalScreenMenuId").OnTable("Departments").AsInt32().WithDefaultValue(0);
            Create.Column("PageCount").OnTable("ScreenMenuCategories").AsInt32().WithDefaultValue(1);
            Create.Column("PageCount").OnTable("TableScreens").AsInt32().WithDefaultValue(1);
            Create.Column("ColumnCount").OnTable("TableScreens").AsInt32().WithDefaultValue(0);
            Create.Column("ButtonHeight").OnTable("TableScreens").AsInt32().WithDefaultValue(80);
            Create.Column("CharsPerLine").OnTable("Printers").AsInt32().WithDefaultValue(42);
            Create.Column("CodePage").OnTable("Printers").AsInt32().WithDefaultValue(857);
            Rename.Column("AlaCarte").OnTable("Departments").To("IsAlaCarte");
            Rename.Column("FastFood").OnTable("Departments").To("IsFastFood");
            Rename.Column("TakeAway").OnTable("Departments").To("IsTakeAway");
        }

        public override void Down()
        {
            Delete.Column("TerminalTableScreenId").FromTable("Departments");
            Delete.Column("TableScreenId").FromTable("Departments");
            Delete.Column("ScreenMenuId").FromTable("Departments");
            Delete.Column("TerminalScreenMenuId").FromTable("Departments");
            Delete.Column("PageCount").FromTable("ScreenMenuCategories");
            Delete.Column("PageCount").FromTable("TableScreens");
            Delete.Column("ColumnCount").FromTable("TableScreens");
            Delete.Column("ButtonHeight").FromTable("TableScreens");
            Delete.Column("CodePage").FromTable("Printers");
            Delete.Column("CharsPerLine").FromTable("Printers");
            Rename.Column("IsAlaCarte").OnTable("Departments").To("AlaCarte");
            Rename.Column("IsFastFood").OnTable("Departments").To("FastFood");
            Rename.Column("IsTakeAway").OnTable("Departments").To("TakeAway");
        }
    }
}