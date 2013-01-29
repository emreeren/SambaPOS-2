using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(9)]
    public class Migration_009 : Migration
    {
        public override void Up()
        {
            Delete.Column("SourceId").FromTable("Discounts");

            Create.Column("SaveFreeTags").OnTable("TicketTagGroups").AsBoolean().WithDefaultValue(true);

            Create.Column("ExcludeVat").OnTable("PrintJobs").AsBoolean().WithDefaultValue(false);

            Create.Column("Tag").OnTable("ScreenMenuItems").AsString(128).Nullable();
            Create.Column("UsageCount").OnTable("ScreenMenuItems").AsInt32().WithDefaultValue(0);
            Create.Column("ItemPortion").OnTable("ScreenMenuItems").AsString(128).Nullable();
            Create.Column("SubButtonHeight").OnTable("ScreenMenuCategories").AsInt32().WithDefaultValue(65);
            Create.Column("MaxItems").OnTable("ScreenMenuCategories").AsInt32().WithDefaultValue(0);
            Create.Column("SortType").OnTable("ScreenMenuCategories").AsInt32().WithDefaultValue(0);
            Create.Column("VatAmount").OnTable("TicketItemProperties").AsDecimal(16, 2).WithDefaultValue(0);

            Create.Column("VatRate").OnTable("TicketItems").AsDecimal(16, 2).WithDefaultValue(0);
            Create.Column("VatAmount").OnTable("TicketItems").AsDecimal(16, 2).WithDefaultValue(0);
            Create.Column("VatTemplateId").OnTable("TicketItems").AsInt32().WithDefaultValue(0);
            Create.Column("VatIncluded").OnTable("TicketItems").AsBoolean().WithDefaultValue(false);

            Create.Column("VatTemplate_Id").OnTable("MenuItems").AsInt32().Nullable();

            Create.Table("VatTemplates")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Rate").AsDecimal(16, 2)
                .WithColumn("VatIncluded").AsBoolean().WithDefaultValue(false);

            Create.ForeignKey("MenuItem_VatTemplate")
                .FromTable("MenuItems").ForeignColumn("VatTemplate_Id")
                .ToTable("VatTemplates").PrimaryColumn("Id");

            Create.Table("TaxServiceTemplates")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("Order").AsInt32().WithDefaultValue(0)
                .WithColumn("CalculationMethod").AsInt32().WithDefaultValue(0)
                .WithColumn("Amount").AsDecimal(16, 2).WithDefaultValue(0);

            Create.Table("DepartmentTaxServiceTemplates")
                .WithColumn("Department_Id").AsInt32().WithDefaultValue(0)
                .WithColumn("TaxServiceTemplate_Id").AsInt32().WithDefaultValue(0);

            Create.ForeignKey("Department_TaxServiceTemplates_Target")
                .FromTable("DepartmentTaxServiceTemplates").ForeignColumn("TaxServiceTemplate_Id")
                .ToTable("TaxServiceTemplates").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);

            Create.ForeignKey("Department_TaxServiceTemplates_Source")
                .FromTable("DepartmentTaxServiceTemplates").ForeignColumn("Department_Id")
                .ToTable("Departments").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);

            Create.Table("TaxServices")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("TicketId").AsInt32().WithDefaultValue(0)
                .WithColumn("TaxServiceId").AsInt32().WithDefaultValue(0)
                .WithColumn("TaxServiceType").AsInt32().WithDefaultValue(0)
                .WithColumn("CalculationType").AsInt32().WithDefaultValue(0)
                .WithColumn("Amount").AsDecimal(16, 2).WithDefaultValue(0)
                .WithColumn("CalculationAmount").AsDecimal(16, 2).WithDefaultValue(0);

            Create.ForeignKey("Ticket_TaxServices")
                .FromTable("TaxServices").ForeignColumn("TicketId")
                .ToTable("Tickets").PrimaryColumn("Id")
                .OnDelete(Rule.Cascade);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}
