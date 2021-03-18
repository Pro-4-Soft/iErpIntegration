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
    public class CustomerReturnDownload : BaseWorker
    {
        public CustomerReturnDownload(ScheduleSetting settings) : base(settings)
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
                    var timeUtc = cache.LastCustomerReturnDownload ?? DateTime.UtcNow.Subtract(TimeSpan.FromDays(100));

                    var orders = await site.WebInvokeAsync<List<Dto.iERP.CustomerReturn>>("IERPOperatSrv_DevolucionesComp/GetDevolucionesAsync", "List", Method.POST, new
                    {
                        SearchEP_Id_Empresa = site.ErpClientId,
                        SearchOC_Fecha_Habilitacion = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")),
                        SearchOE_Id_OC_Estatus = site.PurchaseOrderStatusForDownload,
                        UserName = "p4w@asptg.com",
                        OriginRoute = "P4WGet",
                        OriginMethod = "GetP4WAsync",
                    });
                    if (!orders.Any())
                        return;

                    var clientId = await GetClientIdAsync(site.ClientName);
                    foreach (var order in orders)
                    {
                        try
                        {
                            var customerId = await IdLookupAsync("odata/Customer", $"CustomerCode eq '{HttpUtility.UrlEncode(order.CustomerCode.Trim())}' and ClientId eq {(clientId == null ? "null" : $"{clientId}")}");
                            if (customerId == null)
                            {
                                var cust = await Singleton<Web>.Instance.PostInvokeAsync<dynamic>("api/CustomerApi/CreateOrUpdate", new
                                {
                                    ClientId = clientId,
                                    order.CustomerCode,
                                    CompanyName = order.CustomerName,
                                    //order.Email,
                                    //order.Phone,
                                });
                                customerId = cust.Id;
                                await LogAsync($"Customer [{cust.CustomerCode}] {(string.IsNullOrWhiteSpace(site.ClientName) ? "" : $"for [{site.ClientName}]")} created");
                            }

                            //Order already exists in WMS, skip it
                            var rmas = await Singleton<Web>.Instance.GetInvokeAsync<List<CustomerReturn>>($@"odata/CustomerReturn?
$select=Id,CustomerReturnState,CustomerReturnNumber
&$filter=CustomerReturnNumber eq '{order.CustomerReturnNumber}' and CustomerId eq {customerId} and {(string.IsNullOrWhiteSpace(site.ClientName) ?
                                "ClientId eq null" :
                                $"Client/Name eq '{site.ClientName}'")}");
                            if (rmas.Any())
                                continue;

                            var payload = new
                            {
                                ClientId = clientId,
                                CustomerId = customerId,
                                site.WarehouseCode,
                                order.CustomerReturnNumber,
                                order.ReferenceNumber,
                                Lines = order.Lines
                                    .Select(c => new
                                    {
                                        c.Sku,
                                        c.Quantity
                                    }).ToList()
                            };
                            if (payload.Lines.Count > 0)
                            {
                                await Singleton<Web>.Instance.PostInvokeAsync("api/CustomerReturnApi/CreateOrUpdate", payload);
                                await LogAsync($"RMA: [{order.CustomerReturnNumber}] downloaded!");
                            }
                            else
                                await LogAsync($"RMA: [{order.CustomerReturnNumber}] skipped, no valid lines on an order!");
                        }
                        catch (Exception e)
                        {
                            await LogErrorAsync(e);
                        }
                    }

                    cache.LastCustomerReturnDownload = DateTime.UtcNow;
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
