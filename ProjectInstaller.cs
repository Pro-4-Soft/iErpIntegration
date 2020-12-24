using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using Pro4Soft.iErpIntegration.Infrastructure;

namespace Pro4Soft.iErpIntegration
{
	[RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private readonly Container _components = null;

        public static string ServiceName => $"P4W_iERP_{typeof(BusinessWebException).Assembly.GetName().Version}";
        public static string DisplayName => $"P4W iERP Integration {typeof(BusinessWebException).Assembly.GetName().Version}";
        public static string DisplayDescription => $"P4 Warehouse to iERP Integration version: {typeof(BusinessWebException).Assembly.GetName().Version}";

        public ProjectInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Username = null;

            serviceInstaller.Description = DisplayDescription;
            serviceInstaller.DisplayName = DisplayName;
            serviceInstaller.ServiceName = ServiceName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.AddRange(new Installer[]
            {
                serviceProcessInstaller,
                serviceInstaller
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _components?.Dispose();
            base.Dispose(disposing);
        }
    }
}
