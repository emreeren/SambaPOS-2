using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(4)]
    public class Migration_004 : Migration
    {
        public override void Up()
        {
            Delete.ForeignKey("Terminal_AlaCartePrinter").OnTable("Terminals");
            Delete.ForeignKey("Terminal_FastFoodPrinter").OnTable("Terminals");
            Delete.ForeignKey("Terminal_KitchenPrinter").OnTable("Terminals");
            Delete.ForeignKey("Terminal_TakeAwayPrinter").OnTable("Terminals");
            Delete.ForeignKey("Terminal_TicketPrinter").OnTable("Terminals");
            Delete.ForeignKey("Terminal_AlaCartePrinterTemplate").OnTable("Terminals");
            Delete.ForeignKey("Terminal_FastFoodPrinterTemplate").OnTable("Terminals");
            Delete.ForeignKey("Terminal_KitchenPrinterTemplate").OnTable("Terminals");
            Delete.ForeignKey("Terminal_TakeAwayPrinterTemplate").OnTable("Terminals");
            Delete.ForeignKey("Terminal_TicketPrinterTemplate").OnTable("Terminals");

            Delete.Column("DepartmentType").FromTable("PrinterMaps");
            Delete.Column("TicketPrinter_Id").FromTable("Terminals");
            Delete.Column("KitchenPrinter_Id").FromTable("Terminals");
            Delete.Column("AlaCartePrinter_Id").FromTable("Terminals");
            Delete.Column("TakeAwayPrinter_Id").FromTable("Terminals");
            Delete.Column("FastFoodPrinter_Id").FromTable("Terminals");
            Delete.Column("TicketPrinterTemplate_Id").FromTable("Terminals");
            Delete.Column("KitchenPrinterTemplate_Id").FromTable("Terminals");
            Delete.Column("AlaCartePrinterTemplate_Id").FromTable("Terminals");
            Delete.Column("TakeAwayPrinterTemplate_Id").FromTable("Terminals");
            Delete.Column("FastFoodPrinterTemplate_Id").FromTable("Terminals");

            Create.Column("PrintJob_Id").OnTable("PrinterMaps").AsInt32().Nullable();
            Create.Column("PageHeight").OnTable("Printers").AsInt32().WithDefaultValue(0).NotNullable();

            Alter.Column("HeaderTemplate").OnTable("PrinterTemplates").AsString(500).Nullable();
            Alter.Column("LineTemplate").OnTable("PrinterTemplates").AsString(500).Nullable();
            Alter.Column("VoidedLineTemplate").OnTable("PrinterTemplates").AsString(500).Nullable();
            Alter.Column("GiftLineTemplate").OnTable("PrinterTemplates").AsString(500).Nullable();
            Alter.Column("FooterTemplate").OnTable("PrinterTemplates").AsString(1000).Nullable();

            Create.Column("PrintJobData").OnTable("Tickets").AsString(128).Nullable();
            Delete.Column("LastUpdateTime").FromTable("Tickets");
            Create.Column("LastUpdateTime").OnTable("Tickets").AsDateTime().WithDefaultValue(new DateTime(2000, 1, 1));

            Create.Table("PrintJobs")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Name").AsString(128).Nullable()
                .WithColumn("ButtonText").AsString(128).Nullable()
                .WithColumn("Order").AsInt32().WithDefaultValue(0)
                .WithColumn("AutoPrintIfCash").AsBoolean().WithDefaultValue(false)
                .WithColumn("AutoPrintIfCreditCard").AsBoolean().WithDefaultValue(false)
                .WithColumn("AutoPrintIfTicket").AsBoolean().WithDefaultValue(false)
                .WithColumn("WhenToPrint").AsInt32().WithDefaultValue(0)
                .WithColumn("WhatToPrint").AsInt32().WithDefaultValue(0)
                .WithColumn("LocksTicket").AsBoolean().WithDefaultValue(false)
                .WithColumn("UseFromPaymentScreen").AsBoolean().WithDefaultValue(false)
                .WithColumn("UseFromTerminal").AsBoolean().WithDefaultValue(false);

            Create.ForeignKey("PrintJob_PrinterMaps")
                .FromTable("PrinterMaps").ForeignColumn("PrintJob_Id")
                .ToTable("PrintJobs").PrimaryColumn("Id");

            Create.Table("TerminalPrintJobs")
                .WithColumn("Terminal_Id").AsInt32().WithDefaultValue(0)
                .WithColumn("PrintJob_Id").AsInt32().WithDefaultValue(0);

            Create.ForeignKey("Terminal_PrintJobs_Source")
                .FromTable("TerminalPrintJobs").ForeignColumn("Terminal_Id")
                .ToTable("Terminals").PrimaryColumn("Id");

            Create.ForeignKey("Terminal_PrintJobs_Target")
                .FromTable("TerminalPrintJobs").ForeignColumn("PrintJob_Id")
                .ToTable("PrintJobs").PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}
