using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(6)]
    public class Migration_006 : Migration
    {
        public override void Up()
        {
            Execute.Sql("delete from ScreenMenuItems where ScreenMenuCategory_Id in(select Id from ScreenMenuCategories where ScreenMenu_Id is null)");
            Execute.Sql("delete from ScreenMenuItems where ScreenMenuCategory_Id is null");
            Execute.Sql("delete from ScreenMenuCategories where ScreenMenu_Id is null");
            Execute.Sql("delete from PrinterMaps where PrintJob_Id is null");

            Delete.ForeignKey("ScreenMenu_Categories").OnTable("ScreenMenuCategories");
            Create.Column("ScreenMenuId").OnTable("ScreenMenuCategories").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update ScreenMenuCategories set ScreenMenuId=ScreenMenu_Id");
            Delete.Column("ScreenMenu_Id").FromTable("ScreenMenuCategories");
            Create.ForeignKey("ScreenMenu_Categories")
                .FromTable("ScreenMenuCategories").ForeignColumn("ScreenMenuId")
                .ToTable("ScreenMenus").PrimaryColumn("Id");

            Delete.ForeignKey("ScreenMenuCategory_ScreenMenuItems").OnTable("ScreenMenuItems");
            Create.Column("ScreenMenuCategoryId").OnTable("ScreenMenuItems").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update ScreenMenuItems set ScreenMenuCategoryId=ScreenMenuCategory_Id");
            Delete.Column("ScreenMenuCategory_Id").FromTable("ScreenMenuItems");
            Create.ForeignKey("ScreenMenuCategory_ScreenMenuItems")
                .FromTable("ScreenMenuItems").ForeignColumn("ScreenMenuCategoryId")
                .ToTable("ScreenMenuCategories").PrimaryColumn("Id");

            if (LocalSettings.ConnectionString.EndsWith(".sdf"))
                Delete.ForeignKey("PrintJob_PrinterMaps").OnTable("PrinterMaps");
            else
            {
                Execute.Sql(
@"DECLARE @default sysname, @sql nvarchar(max);

SELECT @default = CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
WHERE COLUMN_NAME= 'PrintJob_Id' AND TABLE_NAME='PrinterMaps';

SET @sql = N'ALTER TABLE [PrinterMaps] DROP CONSTRAINT ' + @default;
EXEC sp_executesql @sql;");
            }

            Create.Column("PrintJobId").OnTable("PrinterMaps").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update PrinterMaps set PrintJobId=PrintJob_Id");
            Delete.Column("PrintJob_Id").FromTable("PrinterMaps");
            Create.ForeignKey("PrintJob_PrinterMaps")
                .FromTable("PrinterMaps").ForeignColumn("PrintJobId")
                .ToTable("PrintJobs").PrimaryColumn("Id");

            Execute.Sql("update Tickets set Tag = Tag+':'+Tag where Tag is not null and Tag != '' and CHARINDEX('#',Tag) = 0 and CHARINDEX(':',Tag) = 0 ");
            Delete.Table("TicketTags");

            Execute.Sql("Delete from TicketItemProperties where TicketItem_Id is null");

            Delete.ForeignKey("TicketItem_Properties").OnTable("TicketItemProperties");
            Create.Column("TicketItemId").OnTable("TicketItemProperties").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update TicketItemProperties set TicketItemId=TicketItem_Id");
            Delete.Column("TicketItem_Id").FromTable("TicketItemProperties");
            Create.ForeignKey("TicketItem_Properties")
                .FromTable("TicketItemProperties").ForeignColumn("TicketItemId")
                .ToTable("TicketItems").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);

            Create.Table("DepartmentTicketTagGroups")
                .WithColumn("Department_Id").AsInt32().PrimaryKey()
                .WithColumn("TicketTagGroup_Id").AsInt32().PrimaryKey();

            Create.Table("TicketTags")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("AccountId").AsInt32().WithDefaultValue(0)
                .WithColumn("AccountName").AsString(128).Nullable()
                .WithColumn("TicketTagGroupId").AsInt32().WithDefaultValue(0).NotNullable();

            Create.Table("TicketTagGroups")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Order").AsInt32().WithDefaultValue(0)
                .WithColumn("Action").AsInt32().WithDefaultValue(0)
                .WithColumn("FreeTagging").AsBoolean().WithDefaultValue(false)
                .WithColumn("ButtonColorWhenTagSelected").AsString(128).Nullable()
                .WithColumn("ButtonColorWhenNoTagSelected").AsString(128).Nullable()
                .WithColumn("ActiveOnPosClient").AsBoolean().WithDefaultValue(false)
                .WithColumn("ActiveOnTerminalClient").AsBoolean().WithDefaultValue(false)
                .WithColumn("ForceValue").AsBoolean().WithDefaultValue(false)
                .WithColumn("NumericTags").AsBoolean().WithDefaultValue(false)
                .WithColumn("Numerator_Id").AsInt32().Nullable();

            Create.ForeignKey("Department_TicketTagGroups_Target")
                .FromTable("DepartmentTicketTagGroups").ForeignColumn("TicketTagGroup_Id")
                .ToTable("TicketTagGroups").PrimaryColumn("Id");

            Create.ForeignKey("TicketTagGroup_Numerator")
                .FromTable("TicketTagGroups").ForeignColumn("Numerator_Id")
                .ToTable("Numerators").PrimaryColumn("Id");

            Create.ForeignKey("TicketTagGroup_TicketTags")
                .FromTable("TicketTags").ForeignColumn("TicketTagGroupId")
                .ToTable("TicketTagGroups").PrimaryColumn("Id");

            Create.Column("DefaultTag").OnTable("Departments").AsString(128).Nullable();
            Create.Column("TerminalDefaultTag").OnTable("Departments").AsString(128).Nullable();

            Create.Column("MenuItemId").OnTable("MenuItemProperties").AsInt32().WithDefaultValue(0);

            Create.Column("MultipleSelection").OnTable("MenuItemPropertyGroups").AsBoolean().WithDefaultValue(false);
            Create.Column("ColumnCount").OnTable("MenuItemPropertyGroups").AsInt32().WithDefaultValue(0);
            Create.Column("ButtonHeight").OnTable("MenuItemPropertyGroups").AsInt32().WithDefaultValue(0);
            Create.Column("ButtonColor").OnTable("MenuItemPropertyGroups").AsString(128).WithDefaultValue("LightGray");
            Create.Column("TerminalColumnCount").OnTable("MenuItemPropertyGroups").AsInt32().WithDefaultValue(0);
            Create.Column("TerminalButtonHeight").OnTable("MenuItemPropertyGroups").AsInt32().WithDefaultValue(0);
            Create.Column("CalculateWithParentPrice").OnTable("MenuItemPropertyGroups").AsBoolean().WithDefaultValue(false);

            Execute.Sql("Update MenuItemPropertyGroups set ColumnCount=5, ButtonHeight=65, ButtonColor='LightGray', TerminalColumnCount=4, TerminalButtonHeight=35");

            Create.Column("FixedCost").OnTable("Recipes").AsDecimal(16, 2).WithDefaultValue(0);

            Create.Column("DefaultProperties").OnTable("ScreenMenuItems").AsString(128).Nullable();

            Create.Column("Quantity").OnTable("TicketItemProperties").AsDecimal(16, 2).WithDefaultValue(0);
            Create.Column("MenuItemId").OnTable("TicketItemProperties").AsInt32().WithDefaultValue(0);
            Create.Column("PortionName").OnTable("TicketItemProperties").AsString(128).Nullable();
            Create.Column("CalculateWithParentPrice").OnTable("TicketItemProperties").AsBoolean().WithDefaultValue(false);

            Create.Column("UseForPaidTickets").OnTable("PrintJobs").AsBoolean().WithDefaultValue(false);
            Create.Column("UseFromPos").OnTable("PrintJobs").AsBoolean().WithDefaultValue(false);
            Execute.Sql("Update PrintJobs set UseFromPos=1");
        }

        public override void Down()
        {
            //do nothing
        }

    }
}
