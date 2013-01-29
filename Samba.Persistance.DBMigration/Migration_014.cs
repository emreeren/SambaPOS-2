using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(14)]
    public class Migration_014 : Migration
    {
        public override void Up()
        {
            Create.Column("DepartmentId").OnTable("TicketItems").AsInt32().WithDefaultValue(0);
            Create.Column("DepartmentId").OnTable("Terminals").AsInt32().WithDefaultValue(0);
            Create.Column("DepartmentId").OnTable("Payments").AsInt32().WithDefaultValue(0);

            if (LocalSettings.ConnectionString.EndsWith(".sdf"))
            {
                for (var i = 1; i <= 9; i++)
                {
                    Execute.Sql(string.Format("Update TicketItems set DepartmentId = {0} Where TicketId IN (SELECT Id from Tickets where DepartmentId = {0})", i));
                    Execute.Sql(string.Format("Update Payments set DepartmentId = {0} Where Ticket_Id IN (SELECT Id from Tickets where DepartmentId = {0})", i));
                }
            }
            else
            {
                Execute.Sql("Update TicketItems set DepartmentId = (Select DepartmentId From Tickets Where Id = TicketItems.TicketId)");
                Execute.Sql("Update Payments set DepartmentId = (Select DepartmentId From Tickets Where Id = Payments.Ticket_Id)");
            }
        }

        public override void Down()
        {
            //do nothing
        }
    }
}