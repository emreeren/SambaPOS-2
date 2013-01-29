using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Inventory;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.InventoryModule
{
    class TransactionListViewModel : EntityCollectionViewModelBase<TransactionViewModel, Transaction>
    {
        protected override TransactionViewModel CreateNewViewModel(Transaction model)
        {
            return new TransactionViewModel(model);
        }

        protected override Transaction CreateNewModel()
        {
            return new Transaction();
        }

        protected override bool CanAddItem(object obj)
        {
            return AppServices.MainDataContext.CurrentWorkPeriod != null;
        }
    }
}
