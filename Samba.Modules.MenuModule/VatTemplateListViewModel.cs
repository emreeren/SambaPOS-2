using System;
using Samba.Domain.Models.Menus;
using Samba.Presentation.Common.ModelBase;

namespace Samba.Modules.MenuModule
{
    class VatTemplateListViewModel : EntityCollectionViewModelBase<VatTemplateViewModel, VatTemplate>
    {
        protected override VatTemplateViewModel CreateNewViewModel(VatTemplate model)
        {
            return new VatTemplateViewModel(model);
        }

        protected override VatTemplate CreateNewModel()
        {
            return new VatTemplate();
        }
    }
}
