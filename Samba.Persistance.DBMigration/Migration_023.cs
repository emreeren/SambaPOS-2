using System;
using System.Collections.Generic;
using System.Text;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    /*
     * This class support employee scheding table
     * Disable multiple item selections from item list view
     * Creating column for early checkin time
     */
    [Migration(23)]
    public class Migration_023 : Migration
	{
        public override void Up()
        {
            
            if (!Schema.Table("Tickets").Column("TerminalId").Exists())
            {
                Create.Column("TerminalId").OnTable("Tickets").AsInt32().WithDefaultValue(1);
                Create.ForeignKey("FK_Tickets_TerminalId")
                .FromTable("Tickets").ForeignColumn("TerminalId")
                .ToTable("Terminals").PrimaryColumn("Id");

              
            }
            if (!Schema.Table("CashTransactions").Column("TerminalId").Exists())
            {
                Create.Column("TerminalId").OnTable("CashTransactions").AsInt32().WithDefaultValue(1);
                Create.ForeignKey("FK_CashTransactions_TerminalId")
                .FromTable("CashTransactions").ForeignColumn("TerminalId")
                .ToTable("Terminals").PrimaryColumn("Id");
            }
            if (!Schema.Table("AccountTransactions").Column("TerminalId").Exists())
            {
                Create.Column("TerminalId").OnTable("AccountTransactions").AsInt32().WithDefaultValue(1);
                Create.ForeignKey("FK_AccountTransactions_TerminalId")
                .FromTable("AccountTransactions").ForeignColumn("TerminalId")
                .ToTable("Terminals").PrimaryColumn("Id");
            }
            if (!Schema.Table("WorkPeriods").Column("TerminalId").Exists())
            {
                Create.Column("TerminalId").OnTable("WorkPeriods").AsInt32().WithDefaultValue(1);
                Create.ForeignKey("FK_WorkPeriods_TerminalId")
                .FromTable("WorkPeriods").ForeignColumn("TerminalId")
                .ToTable("Terminals").PrimaryColumn("Id");
            }
            if (!Schema.Table("Transactions").Column("TerminalId").Exists())
            {
                Create.Column("TerminalId").OnTable("Transactions").AsInt32().WithDefaultValue(1);
                Create.ForeignKey("FK_Transactions_TerminalId")
                .FromTable("Transactions").ForeignColumn("TerminalId")
                .ToTable("Terminals").PrimaryColumn("Id");
            }
            
            
           

        }
        public override void Down()
        {
            //do nothing
        }

	}
}
