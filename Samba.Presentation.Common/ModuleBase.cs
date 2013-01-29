using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.ServiceLocation;

namespace Samba.Presentation.Common
{
    public abstract class ModuleBase : IModule
    {
        public void Initialize()
        {
            var moduleLifecycleService = ServiceLocator.Current.GetInstance<IModuleInitializationService>();
            moduleLifecycleService.RegisterForStage(OnPreInitialization, ModuleInitializationStage.PreInitialization);
            moduleLifecycleService.RegisterForStage(OnInitialization, ModuleInitializationStage.Initialization);
            moduleLifecycleService.RegisterForStage(OnPostInitialization, ModuleInitializationStage.PostInitialization);
            moduleLifecycleService.RegisterForStage(OnStartUp, ModuleInitializationStage.StartUp);
        }

        protected virtual void OnPreInitialization()
        {
        }

        protected virtual void OnInitialization()
        {
        }

        protected virtual void OnPostInitialization()
        {
        }

        protected virtual void OnStartUp()
        {
        }
    }
}