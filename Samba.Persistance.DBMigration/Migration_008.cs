using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(8)]
    public class Migration_008 : Migration
    {
        public override void Up()
        {
            Create.Column("PriceTag").OnTable("Departments").AsString(10).Nullable();
            Create.Column("Tag").OnTable("MenuItems").AsString(128).Nullable();
            Create.Column("PriceTag").OnTable("TicketItems").AsString(10).Nullable();
            Create.Column("Tag").OnTable("TicketItems").AsString(128).Nullable();

            Create.Table("ActionContainers")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("AppActionId").AsInt32().WithDefaultValue(0)
                .WithColumn("AppRuleId").AsInt32().WithDefaultValue(0)
                .WithColumn("ParameterValues").AsString(500).Nullable()
                .WithColumn("Order").AsInt32().WithDefaultValue(0);

            Create.Table("AppActions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("ActionType").AsString(128).Nullable()
                .WithColumn("Parameter").AsString(500).Nullable();

            Create.Table("AppRules")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("EventName").AsString(128).Nullable()
                .WithColumn("EventConstraints").AsString(500).Nullable();

            Create.Table("MenuItemPriceDefinitions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("PriceTag").AsString(10).Nullable();

            Create.Table("MenuItemPrices")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("MenuItemPortionId").AsInt32().WithDefaultValue(0)
                .WithColumn("PriceTag").AsString(10).Nullable()
                .WithColumn("Price").AsDecimal(16, 2).WithDefaultValue(0);

            Create.Table("Triggers")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Expression").AsString(128).Nullable()
                .WithColumn("LastTrigger").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

            Create.Table("AccountTransactions")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Date").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1))
                .WithColumn("TransactionType").AsInt32().WithDefaultValue(0)
                .WithColumn("Amount").AsDecimal(16, 2).WithDefaultValue(0)
                .WithColumn("UserId").AsInt32().WithDefaultValue(0)
                .WithColumn("CustomerId").AsInt32().WithDefaultValue(0);
            
            Create.ForeignKey("AppRule_Actions")
                .FromTable("ActionContainers").ForeignColumn("AppRuleId")
                .ToTable("AppRules").PrimaryColumn("Id");

            Create.ForeignKey("MenuItemPortion_Prices")
                .FromTable("MenuItemPrices").ForeignColumn("MenuItemPortionId")
                .ToTable("MenuItemPortions").PrimaryColumn("Id").OnDelete(Rule.Cascade);

            Create.Index("IX_Tickets_LastPaymentDate").OnTable("Tickets").OnColumn("LastPaymentDate").Ascending()
                .WithOptions().NonClustered();
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
