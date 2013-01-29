using System;
using System.Collections.Generic;
using Samba.Domain.Models.Users;
using Samba.Infrastructure;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;
using System.Linq;

namespace Samba.Modules.UserModule
{
    public class UserViewModel : EntityViewModelBase<User>
    {
        private bool _edited;

        public UserViewModel(User user)
            : base(user)
        {
            EventServiceFactory.EventService.GetEvent<GenericEvent<UserRole>>().Subscribe(x => RaisePropertyChanged("Roles"));
        }

        public string PinCode
        {
            get
            {
                if (_edited) return Model.PinCode;
                return !string.IsNullOrEmpty(Model.PinCode) ? "********" : "";
            }
            set
            {
                if (Model.PinCode == null || !Model.PinCode.Contains("*") && !string.IsNullOrEmpty(value))
                {
                    _edited = true;
                    Model.PinCode = value;
                    RaisePropertyChanged("PinCode");
                }
            }
        }

        public UserRole Role { get { return Model.UserRole; } set { Model.UserRole = value; } }

        public IEnumerable<UserRole> Roles { get; private set; }

        public override Type GetViewType()
        {
            return typeof(UserView);
        }

        public override string GetModelTypeString()
        {
            return Resources.User;
        }

        protected override void Initialize(IWorkspace workspace)
        {
            Roles = workspace.All<UserRole>();
        }

        protected override string GetSaveErrorMessage()
        {
            var users = AppServices.Workspace.All<User>(x => x.PinCode == Model.PinCode);
            return users.Count() > 1 || (users.Count() == 1 && users.ElementAt(0).Id != Model.Id)
                ? Resources.SaveErrorThisPinCodeInUse : "";
        }
    }
}
