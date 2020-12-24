using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

namespace Pro4Soft.iErpIntegration.Workers.Download
{
    public class ProductDownload : BaseWorker
    {
        public ProductDownload(ScheduleSetting settings) : base(settings) { }

        public override void Execute()
        {
            ExecuteAsync().Wait();
        }

        private async Task ExecuteAsync()
        {
            foreach (var site in App<Settings>.Instance.Sites)
            {
                try
                {
                    var cache = App<Cache>.Instance[site.Name];
                    var result = (await site.WebInvokeAsync<List<Dto.iERP.Product>>("IERPOperatSrv_ProductosComp/GetProductosAsync", "List", Method.POST, new
                    {
                        EP_Id_Empresa = site.ErpClientId,
                        SearchPR_Fecha = cache.LastProductDownload ?? DateTime.UtcNow.Subtract(TimeSpan.FromDays(100))
                    })).Where(c => c.Status == "Activo").ToList();

                    if (!result.Any())
                        return;

                    var clientId = await GetClientIdAsync(site.ClientName);
                    foreach (var prod in result)
                    {
                        try
                        {
                            var resp = await Singleton<Web>.Instance.PostInvokeAsync<Dto.P4W.Product >("api/ProductApi/CreateOrUpdate", new
                            {
                                prod.Sku,
                                ReferenceNumber = prod.Code,
                                prod.Description,
                                ClientId = clientId,
                                Upc = prod.Sku,
                                prod.Category
                            });

                            await LogAsync($"{resp.Sku} downloaded{(string.IsNullOrWhiteSpace(site.ClientName) ? "" : $" for [{site.ClientName}]")}");
                        }
                        catch (Exception e)
                        {
                            await LogErrorAsync(e);
                        }
                    }

                    cache.LastProductDownload = DateTime.UtcNow;
                    App<Cache>.Instance.SaveToFile();
                }
                catch (Exception e)
                {
                    await LogErrorAsync(e);
                }
            }
        }
    }
}