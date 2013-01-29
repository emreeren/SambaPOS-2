using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;

namespace Samba.Login
{
    [ModuleExport(typeof(LoginModule))]
    public class LoginModule : IModule
    {
        readonly IRegionManager _regionManager;

        [ImportingConstructor]
        public LoginModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void Initialize()
        {
            _regionManager.RegisterViewWithRegion("MainRegion", typeof(LoginView));
        }
    }
}
