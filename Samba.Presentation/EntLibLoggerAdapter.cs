using System;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.Prism.Logging;

namespace Samba.Presentation
{
    public class EntLibLoggerAdapter : ILoggerFacade
    {
        public void Log(string message, Category category, Priority priority)
        {
            try
            {
                Logger.Write(message, category.ToString(), (int)priority);
            }
            catch (Exception)
            {
            }
        }
    }
}
