using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Common.ModelBase;

namespace Samba.Modules.MenuModule
{
    class TaxServiceTemplateListViewModel : EntityCollectionViewModelBase<TaxServiceTemplateViewModel, TaxServiceTemplate>
    {
        protected override TaxServiceTemplateViewModel CreateNewViewModel(TaxServiceTemplate model)
        {
            return new TaxServiceTemplateViewModel(model);
        }

        protected override TaxServiceTemplate CreateNewModel()
        {
            return new TaxServiceTemplate();
        }
    }
}
