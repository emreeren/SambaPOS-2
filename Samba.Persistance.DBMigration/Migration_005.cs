using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(5)]
    public class Migration_005 : Migration
    {
        public override void Up()
        {
            Execute.Sql("delete from TicketItemProperties where TicketItem_Id in (select Id from TicketItems where Ticket_Id is null)");
            Execute.Sql("delete from TicketItems where Ticket_Id is null");
            Create.Column("NumeratorHeight").OnTable("TableScreens").AsInt32().WithDefaultValue(0);
            Create.Column("AlphaButtonValues").OnTable("TableScreens").AsString(128).Nullable();
            Create.Column("Tag").OnTable("Tickets").AsString(128).Nullable();
            Create.Column("TicketTag").OnTable("PrinterMaps").AsString(128).Nullable();
            Create.Column("InternalAccount").OnTable("Customers").AsBoolean().WithDefaultValue(false);
            Create.Column("OpenTicketViewColumnCount").OnTable("Departments").AsInt32().WithDefaultValue(5);

            Delete.ForeignKey("Ticket_TicketItems").OnTable("TicketItems");
            Create.Column("TicketId").OnTable("TicketItems").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update TicketItems set TicketId=Ticket_Id");
            Delete.Column("Ticket_Id").FromTable("TicketItems");
            Create.ForeignKey("Ticket_TicketItems")
                .FromTable("TicketItems").ForeignColumn("TicketId")
                .ToTable("Tickets").PrimaryColumn("Id");

            Delete.ForeignKey("UserRole_Permissions").OnTable("Permissions");
            Create.Column("UserRoleId").OnTable("Permissions").AsInt32().WithDefaultValue(0).NotNullable();
            Execute.Sql("Update Permissions set UserRoleId=UserRole_Id");
            Delete.Column("UserRole_Id").FromTable("Permissions");
            Create.ForeignKey("UserRole_Permissions")
                .FromTable("Permissions").ForeignColumn("UserRoleId")
                .ToTable("UserRoles").PrimaryColumn("Id");

            Create.Table("TicketTags")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Numerator_Id").AsInt32().Nullable()
                .WithColumn("Account_Id").AsInt32().Nullable();

            Create.Table("CostItems")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("PeriodicConsumptionId").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Quantity").AsDecimal().WithDefaultValue(0)
                .WithColumn("CostPrediction").AsDecimal().WithDefaultValue(0)
                .WithColumn("Cost").AsDecimal().WithDefaultValue(0)
                .WithColumn("Portion_Id").AsInt32().Nullable();

            Create.Table("InventoryItems")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("GroupCode").AsString(128).Nullable()
                .WithColumn("BaseUnit").AsString(10).Nullable()
                .WithColumn("TransactionUnit").AsString(10).Nullable()
                .WithColumn("TransactionUnitMultiplier").AsInt32().WithDefaultValue(0);

            Create.Table("PeriodicConsumptionItems")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("PeriodicConsumptionId").AsInt32().WithDefaultValue(0)
                .WithColumn("UnitMultiplier").AsDecimal().WithDefaultValue(0)
                .WithColumn("InStock").AsDecimal().WithDefaultValue(0)
                .WithColumn("Purchase").AsDecimal().WithDefaultValue(0)
                .WithColumn("Consumption").AsDecimal().WithDefaultValue(0)
                .WithColumn("PhysicalInventory").AsDecimal().Nullable()
                .WithColumn("Cost").AsDecimal().WithDefaultValue(0)
                .WithColumn("InventoryItem_Id").AsInt32().Nullable();

            Create.Table("PeriodicConsumptions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("WorkPeriodId").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("StartDate").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1))
                .WithColumn("EndDate").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

            Create.Table("RecipeItems")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("RecipeId").AsInt32().WithDefaultValue(0)
                .WithColumn("Quantity").AsDecimal().WithDefaultValue(0)
                .WithColumn("InventoryItem_Id").AsInt32().Nullable();

            Create.Table("Recipes")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Portion_Id").AsInt32().Nullable();

            Create.Table("TransactionItems")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("TransactionId").AsInt32().WithDefaultValue(0)
                .WithColumn("Unit").AsString(128).Nullable()
                .WithColumn("Multiplier").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Quantity").AsDecimal().WithDefaultValue(0)
                .WithColumn("Price").AsDecimal().WithDefaultValue(0)
                .WithColumn("InventoryItem_Id").AsInt32().Nullable();

            Create.Table("Transactions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Date").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

            Create.ForeignKey("TicketTag_Numerator")
                .FromTable("TicketTags").ForeignColumn("Numerator_Id")
                .ToTable("Numerators").PrimaryColumn("Id");

            Create.ForeignKey("CostItem_Portion")
                .FromTable("CostItems").ForeignColumn("Portion_Id")
                .ToTable("MenuItemPortions").PrimaryColumn("Id");

            Create.ForeignKey("PeriodicConsumption_CostItems")
                .FromTable("CostItems").ForeignColumn("PeriodicConsumptionId")
                .ToTable("PeriodicConsumptions").PrimaryColumn("Id");

            Create.ForeignKey("PeriodicConsumptionItem_InventoryItem")
                .FromTable("PeriodicConsumptionItems").ForeignColumn("InventoryItem_Id")
                .ToTable("InventoryItems").PrimaryColumn("Id");

            Create.ForeignKey("RecipeItem_InventoryItem")
                .FromTable("RecipeItems").ForeignColumn("InventoryItem_Id")
                .ToTable("InventoryItems").PrimaryColumn("Id");

            Create.ForeignKey("TransactionItem_InventoryItem")
                .FromTable("TransactionItems").ForeignColumn("InventoryItem_Id")
                .ToTable("InventoryItems").PrimaryColumn("Id");

            Create.ForeignKey("PeriodicConsumption_PeriodicConsumptionItems")
                .FromTable("PeriodicConsumptionItems").ForeignColumn("PeriodicConsumptionId")
                .ToTable("PeriodicConsumptions").PrimaryColumn("Id");

            Create.ForeignKey("Recipe_RecipeItems")
                .FromTable("RecipeItems").ForeignColumn("RecipeId")
                .ToTable("Recipes").PrimaryColumn("Id");

            Create.ForeignKey("Recipe_Portion")
                .FromTable("Recipes").ForeignColumn("Portion_Id")
                .ToTable("MenuItemPortions").PrimaryColumn("Id");

            Create.ForeignKey("Transaction_TransactionItems")
                .FromTable("TransactionItems").ForeignColumn("TransactionId")
                .ToTable("Transactions").PrimaryColumn("Id");
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
