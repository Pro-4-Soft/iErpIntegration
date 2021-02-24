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

        static void Main(string[] args)
        {
            var command = args?.Length > 0 ? args?[0].ToLower().Trim() : null;
            switch (command)
            {
                case "/version":
                    Console.Out.WriteLine(typeof(BusinessWebException).Assembly.GetName().Version.ToString());
                    return;
                case "/install":
                    ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
                    break;
                case "/uninstall":
                    ManagedInstallerClass.InstallHelper(new[] {"/u", Assembly.GetExecutingAssembly().Location});
                    break;
                case "/standalone":
                    try
                    {
                        Singleton<EntryPoint>.Instance.OnStart(args);
                        Console.In.ReadLine();
                        Singleton<EntryPoint>.Instance.OnStop();
                    }
                    catch (Exception ex)
                    {
                        Console.Out.WriteLine(ex.ToString());
                    }
                    break;
                default:
                    Run(new ServiceBase[] {new EntryPoint()});
                    break;
            }
        }

        protected override void OnStart(string[] args)
        {
            Singleton<EntryPoint>.Instance.CloudUrl = ConfigurationManager.AppSettings["CloudUrl"];
            Singleton<EntryPoint>.Instance.ApiKey = ConfigurationManager.AppSettings["ApiKey"];
            Singleton<EntryPoint>.Instance.TenantId = Singleton<Web>.Instance.GetInvokeAsync<TenantDetails>("api/TenantApi/GetTenantDetails").Result.Id;

            Console.Out.WriteLine($"CloudUrl: {Singleton<EntryPoint>.Instance.CloudUrl}");
            Console.Out.WriteLine($"ApiKey: {Singleton<EntryPoint>.Instance.ApiKey}");
            Console.Out.WriteLine($"TenantId: {Singleton<EntryPoint>.Instance.TenantId}");

            ScheduleThread.Instance.Start();
        }

        protected override void OnStop()
        {
            ScheduleThread.Instance.Stop();
        }
    }
}
