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
    public class SalesOrderDownload : BaseWorker
    {
        public SalesOrderDownload(ScheduleSetting settings) : base(settings)
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
                    var result = (await site.WebInvokeAsync<List<Dto.iERP.SalesOrder>>("IERPOperatSrv_CotizacionesComp/GetCotizacionesAsync", "List", Method.POST, new
                    {
                        SearchEP_Id_Empresa = site.ErpClientId,
                        SearchCZ_Fecha_Emision_Desde = cache.LastSalesOrderDownload ?? DateTime.UtcNow.Subtract(TimeSpan.FromDays(100)),
                        SearchCZE_Id_Cotizacion_Estatus = site.SalesOrderStatusForDownload
                    })).ToList();

                    if (!result.Any())
                        return;

                    var clientId = await GetClientIdAsync(site.ClientName);
                    foreach (var order in result)
                    {
                        try
                        {
                            var customerId = await IdLookupAsync("odata/Customer", $"CustomerCode eq '{HttpUtility.UrlEncode(order.CustomerName.Trim())}' and ClientId eq {(clientId == null ? "null" : $"{clientId}")}");
                            if (customerId == null)
                            {
                                var cust = await Singleton<Web>.Instance.PostInvokeAsync<dynamic>("api/CustomerApi/CreateOrUpdate", new
                                {
                                    ClientId = clientId,
                                    CustomerCode = order.CustomerName,
                                    CompanyName = order.CustomerName,
                                    order.Email
                                });
                                customerId = cust.Id;
                                await LogAsync($"Customer [{cust.CustomerCode}] {(string.IsNullOrWhiteSpace(site.ClientName) ? "" : $"for [{site.ClientName}]")} created");
                            }

                            var payload = new
                            {
                                ClientId = clientId,
                                CustomerId = customerId,
                                site.WarehouseCode,
                                PickTicketNumber = order.SalesOrderNumber,
                                order.ReferenceNumber,
                                //ShipToName = order.ShippingAddress?.Name ?? order.BillingAddress?.Name,
                                //ShipToAddress1 = order.ShippingAddress?.Address1 ?? order.BillingAddress?.Address1,
                                //ShipToAddress2 = order.ShippingAddress?.Address2 ?? order.BillingAddress?.Address2,
                                //ShipToCity = order.ShippingAddress?.City ?? order.BillingAddress?.City,
                                //ShipToStateProvince = order.ShippingAddress?.ProvinceCode ?? order.BillingAddress?.ProvinceCode,
                                //ShipToZipPostal = order.ShippingAddress?.Zip ?? order.BillingAddress?.Zip,
                                //ShipToCountry = order.ShippingAddress?.CountryCode ?? order.BillingAddress?.CountryCode,
                                ShipToPhone = order.Phone,
                                ShipToAttnTo = order.ContactPerson,
                                Lines = order.Lines
                                    .Select(c => new
                                    {
                                        c.Sku, 
                                        c.OrderedQuantity
                                    }).ToList()
                            };

                            if (payload.Lines.Count > 0)
                            {
                                await Singleton<Web>.Instance.PostInvokeAsync("api/PickTicketApi/CreateOrUpdate", payload);
                                await LogAsync($"SO: [{order.SalesOrderNumber}] downloaded!");
                            }
                            else
                                await LogAsync($"SO: [{order.SalesOrderNumber}] skipped, no valid lines on an order!");
                        }
                        catch (Exception e)
                        {
                            await LogErrorAsync(e);
                        }
                    }

                    cache.LastSalesOrderDownload = DateTime.UtcNow;
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
