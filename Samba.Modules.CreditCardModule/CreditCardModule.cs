using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.CreditCardModule
{
    [ModuleExport(typeof(CreditCardModule))]
    class CreditCardModule : ModuleBase
    {
        [ImportMany]
        public IEnumerable<ICreditCardProcessor> CreditCardProcessors { get; set; }

        protected override void OnInitialization()
        {
            foreach (var creditCardProcessor in CreditCardProcessors)
            {
                CreditCardProcessingService.RegisterCreditCardProcessor(creditCardProcessor);
            }
        }
    }
}
