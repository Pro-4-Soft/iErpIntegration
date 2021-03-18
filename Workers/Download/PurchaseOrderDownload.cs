using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

namespace Pro4Soft.iErpIntegration.Workers.Download
{
    public class PurchaseOrderDownload : BaseWorker
    {
        public PurchaseOrderDownload(ScheduleSetting settings) : base(settings)
        {
        }

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
                    var result = await site.WebInvokeAsync<List<Dto.iERP.PurchaseOrder>>("IERPOperatSrv_OrdenCompraComp/GetOrdenesComprasAsync", "ListOrdenes", Method.POST, new
                    {
                        SearchEP_Id_Empresa = site.ErpClientId,
                        SearchOC_Fecha_Habilitacion = cache.LastPurchaseOrderDownload ?? DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
                        SearchOE_Id_OC_Estatus = site.PurchaseOrderStatusForDownload
                    });
                    if (!result.Any())
                        return;

                    var clientId = await GetClientIdAsync(site.ClientName);
                    foreach (var order in result)
                    {
                        try
                        {
                            var vendorId = await IdLookupAsync("odata/Vendor", $"CompanyName eq '{HttpUtility.UrlEncode(order.VendorName.Trim())}' and ClientId eq {clientId?.ToString() ?? "null"}");
                            if (vendorId == null)
                            {
                                var resp = await Singleton<Web>.Instance.PostInvokeAsync<dynamic>("api/VendorApi/CreateOrUpdate", new
                                {
                                    ClientId = clientId,
                                    VendorCode = order.VendorName.Trim(),
                                    CompanyName = order.VendorName.Trim(),
                                });
                                vendorId = Guid.TryParse((string) resp.Id.ToString(), out var vendId) ? vendId : throw new BusinessWebException($"Vendor [{order.VendorName}] could not be created");
                                await LogAsync($"Vendor [{order.VendorName}] created");
                            }

                            //Order already exists in WMS, skip it
                            var pos = await Singleton<Web>.Instance.GetInvokeAsync<List<PurchaseOrder>>($@"odata/PurchaseOrder?
$select=Id,PurchaseOrderState,PurchaseOrderNumber
&$filter=PurchaseOrderNumber eq '{order.PurchaseOrderNumber}' and VendorId eq {vendorId} and {(string.IsNullOrWhiteSpace(site.ClientName) ?
                                "ClientId eq null" :
                                $"Client/Name eq '{site.ClientName}'")}");
                            if (pos.Any())
                                continue;

                            var payload = new
                            {
                                ClientId = clientId,
                                VendorId = vendorId,
                                site.WarehouseCode,
                                order.PurchaseOrderNumber,
                                order.ReferenceNumber,
                                Lines = order.Lines
                                    .Select(c => new
                                    {
                                        c.Sku,
                                        c.OrderedQuantity
                                    }).ToList()
                            };
                            if (payload.Lines.Count > 0)
                            {
                                await Singleton<Web>.Instance.PostInvokeAsync("api/PurchaseOrderApi/CreateOrUpdate", payload);
                                await LogAsync($"PO: [{order.PurchaseOrderNumber}] downloaded!");
                            }
                            else
                                await LogAsync($"PO: [{order.PurchaseOrderNumber}] skipped, no valid lines on an order!");
                        }
                        catch (Exception e)
                        {
                            await LogErrorAsync(e);
                        }
                    }

                    cache.LastPurchaseOrderDownload = DateTime.UtcNow;
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
