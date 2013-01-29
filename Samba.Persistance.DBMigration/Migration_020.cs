using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(20)]
    public class Migration_020 : Migration
    {
        public override void Up()
        {
            if (!LocalSettings.ConnectionString.ToLower().Contains(".sdf"))
            {
                Execute.Sql("CREATE NONCLUSTERED INDEX IDX_TicketItemProperties_All ON TicketItemProperties (TicketItemId) INCLUDE (Id,Name,PropertyPrice_CurrencyCode,PropertyPrice_Amount,PropertyGroupId,Quantity,MenuItemId,PortionName,CalculateWithParentPrice,VatAmount)");
                Execute.Sql("CREATE NONCLUSTERED INDEX IDX_Payments_All ON Payments (Ticket_Id) INCLUDE (Id,Amount,Date,PaymentType,UserId,DepartmentId)");
            }
            Create.Column("HideExitButton").OnTable("Terminals").AsBoolean().WithDefaultValue(false);
        }

        public override void Down()
        {
            //do nothing
        }
    }
}