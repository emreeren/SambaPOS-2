using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Customers;
using Samba.Presentation.Common.ModelBase;

namespace Samba.Modules.CustomerModule
{
    class CustomerListViewModel : EntityCollectionViewModelBase<CustomerEditorViewModel, Customer>
    {
        protected override CustomerEditorViewModel CreateNewViewModel(Customer model)
        {
            return new CustomerEditorViewModel(model);
        }

        protected override Customer CreateNewModel()
        {
            return new Customer();
        }
    }
}
