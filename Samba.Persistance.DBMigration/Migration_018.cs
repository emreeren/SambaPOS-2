using FluentMigrator;
using Samba.Infrastructure.Settings;

namespace Samba.Persistance.DBMigration
{
    [Migration(18)]
    public class Migration_018 : Migration
    {
        public override void Up()
        {
            if (!LocalSettings.ConnectionString.ToLower().Contains(".sdf"))
            {
                Execute.Sql("CREATE NONCLUSTERED INDEX IDX_TicketItems_All ON TicketItems (TicketId) INCLUDE (Id,MenuItemId,MenuItemName,PortionName,Price,CurrencyCode,Quantity,PortionCount,Locked,Voided,ReasonId,Gifted,OrderNumber,CreatingUserId,CreatedDateTime,ModifiedUserId,ModifiedDateTime,PriceTag,Tag,DepartmentId,VatRate,VatAmount,VatTemplateId,VatIncluded)");
            }
        }

        public override void Down()
        {
            //do nothing
        }
    }
}