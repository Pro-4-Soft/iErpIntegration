using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;

namespace Pro4Soft.iErpIntegration.Workers.Upload
{
    public class InventorySyncUpload : BaseWorker
    {
        public InventorySyncUpload(ScheduleSetting settings) : base(settings)
        {

        }

        public override void Execute()
        {
            ExecuteAsync().Wait();
        }

        public async Task ExecuteAsync()
        {
            foreach (var site in App<Settings>.Instance.Sites)
            {
                try
                {
                    var time = new Stopwatch();
                    time.Start();

                    var clientId = await GetClientIdAsync(site.ClientName);

                    var p4Items = await Singleton<Web>.Instance.GetInvokeAsync<List<ProductAvailabilityRecord>>($"api/ProductApi/GetAvailableInventory?hasClient=true&clientId={clientId}");

                    //Do some work...

                    await LogAsync($"Products quantities updated {(string.IsNullOrWhiteSpace(site.ClientName) ? "" : $"for [{site.ClientName}]")}. Elapsed time {time.Elapsed}");
                }
                catch (Exception ex)
                {
                    await LogErrorAsync(ex);
                }
            }
        }
    }
}
