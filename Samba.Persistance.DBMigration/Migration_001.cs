using System;
using FluentMigrator;

namespace Samba.Persistance.DBMigration
{
    [Migration(1)]
    public class Migration_001 : Migration
    {
        public override void Up()
        {
            Create.Column("ModifiedDateTime").OnTable("TicketItems").AsDate().WithDefaultValue(new DateTime(2010, 1, 1));
            Create.Column("CreatedDateTime").OnTable("TicketItems").AsDate().WithDefaultValue(new DateTime(2010, 1, 1));
            Create.Column("ModifiedUserId").OnTable("TicketItems").AsInt32().WithDefaultValue(1);
            Delete.Column("GiftedUserId").FromTable("TicketItems");
            Delete.Column("VoidedUserId").FromTable("TicketItems");
        }

        public override void Down()
        {
            Delete.Column("ModifiedDateTime").FromTable("TicketItems");
            Delete.Column("CreatedDateTime").FromTable("TicketItems");
            Delete.Column("ModifiedUserId").FromTable("TicketItems");
            Create.Column("GiftedUserId").OnTable("TicketItems").AsInt32();
            Create.Column("VoidedUserId").OnTable("TicketItems").AsInt32();
        }
    }
}