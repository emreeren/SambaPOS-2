using System;
using System.Collections.Generic;
using Samba.Domain.Models.Customers;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.CustomerModule
{
    public class CustomerEditorViewModel : EntityViewModelBase<Customer>
    {
        public CustomerEditorViewModel(Customer model)
            : base(model)
        {
        }

        public override Type GetViewType()
        {
            return typeof(CustomerEditorView);
        }

        public override string GetModelTypeString()
        {
            return Resources.Customer;
        }

        private IEnumerable<string> _groupCodes;
        public IEnumerable<string> GroupCodes { get { return _groupCodes ?? (_groupCodes = Dao.Distinct<Customer>(x => x.GroupCode)); } }

        public string GroupValue { get { return Model.GroupCode; } }
        public string GroupCode { get { return Model.GroupCode ?? ""; } set { Model.GroupCode = value; } }
        public string PhoneNumber { get { return Model.PhoneNumber; } set { Model.PhoneNumber = value; } }
        public string Address { get { return Model.Address; } set { Model.Address = value; } }
        public string Note { get { return Model.Note; } set { Model.Note = value; } }
        public bool InternalAccount { get { return Model.InternalAccount; } set { Model.InternalAccount = value; } }
        public string PhoneNumberInputMask { get { return AppServices.SettingService.PhoneNumberInputMask; } }
    }
}
