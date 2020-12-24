using System;
using System.Configuration;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

namespace Pro4Soft.iErpIntegration
{
    public class EntryPoint : ServiceBase
    {
        public string[] Args;

        public string ApiKey { get; set; }
        public string CloudUrl { get; set; }
        public Guid TenantId { get; set; }

        private RestClient _client;
        public RestClient Client => _client ??= new RestClient(CloudUrl);

        public void Initialize(string[] args = null)
        {
            Args = args;
            CloudUrl = args?.Length > 0 ? args[0] : ConfigurationManager.AppSettings["CloudUrl"];
            ApiKey = args?.Length > 1 ? args[1] : ConfigurationManager.AppSettings["ApiKey"];
        }

        public void Startup()
        {
            var command = Args?.Length > 0 ? Args?[0].ToLower().Trim() : null;
            switch (command)
            {
                case "/version":
                    Console.Out.WriteLine(typeof(BusinessWebException).Assembly.GetName().Version.ToString());
                    return;
                case "/install":
                    ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                    break;
                case "/uninstall":
                    ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
                    break;
                default:
                {
                    Console.Out.WriteLine($"CloudUrl: {CloudUrl}");
                    Console.Out.WriteLine($"ApiKey: {ApiKey}");

                    try
                    {
                        TenantId = Singleton<Web>.Instance.GetInvokeAsync<TenantDetails>("api/TenantApi/GetTenantDetails").Result.Id;

                        Console.Out.WriteLine($"TenantId: {TenantId}");

                        OnStart(Args);
                        Console.In.ReadLine();
                        OnStop();
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.ToString());
                    }
                    break;
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            ScheduleThread.Instance.Start();
        }

        protected override void OnStop()
        {
            ScheduleThread.Instance.Stop();
        }
    }
}
