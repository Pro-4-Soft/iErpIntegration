﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

namespace Pro4Soft.iErpIntegration.Workers.Upload
{
    public class PurchaseOrderUpload : BaseWorker
    {
        public PurchaseOrderUpload(ScheduleSetting settings) : base(settings)
        {
        }

        public override void Execute()
        {
            Subscribe();
            ExecuteAsync().Wait();
        }

        public async Task ExecuteAsync()
        {
            foreach (var site in App<Settings>.Instance.Sites)
            {
                try
                {
                    var url = $@"odata/PurchaseOrder?
$select=Id,PurchaseOrderNumber,ReferenceNumber
&$expand=Client($select=Id,Name),
        Lines($orderby=LineNumber;
              $select=Id,ReceivedQuantity,ReferenceNumber;
              $expand=Product($select=Id,Sku,ReferenceNumber),
                      LineDetails($orderby=LotNumber,SerialNumber))
&$orderby=PurchaseOrderNumber
&$filter=IsWarehouseTransfer eq false and UploadDate eq null and PurchaseOrderState eq 'Closed' and {(string.IsNullOrWhiteSpace(site.ClientName) ?
                        "ClientId eq null" : 
                        $"Client/Name eq '{site.ClientName}'")}";
                    var pos = await Singleton<Web>.Instance.GetInvokeAsync<List<PurchaseOrder>>(url);
                    if (!pos.Any())
                        continue;

                    foreach (var po in pos)
                    {
                        try
                        {
                            throw new Exception("FAIL TEST");
                            //DO SOME WORK

                            await Singleton<Web>.Instance.PostInvokeAsync("api/PurchaseOrderApi/CreateOrUpdate", new
                            {
                                po.Id,
                                UploadDate = DateTime.UtcNow,
                                UploadedSuceeded = true,
                                UploadMessage = (string)null,
                            });

                            await LogAsync($"PO: [{po.PurchaseOrderNumber}] for [{site.ClientName ?? site.Name}] uploaded");
                        }
                        catch (Exception e)
                        {
                            await Singleton<Web>.Instance.PostInvokeAsync("api/PurchaseOrderApi/CreateOrUpdate", new
                            {
                                po.Id,
                                UploadDate = DateTime.UtcNow,
                                UploadedSuceeded = false,
                                UploadMessage = e.ToString()
                            });
                            await LogErrorAsync(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    await LogErrorAsync(e);
                }
            }
        }

        private static bool _isPrimed;
        private void Subscribe()
        {
            if (_isPrimed)
                return;

            Singleton<WebEventListener>.Instance.Subscribe("ObjectChanged", p =>
            {
                try
                {
                    var changeEvent = Utils.DeserializeFromJson<ObjectChangeEvent>((string)Utils.SerializeToStringJson(p));
                    switch (changeEvent.ObjectType)
                    {
                        case nameof(PurchaseOrder) when changeEvent.Changes.Any(c => c.PropertyName == nameof(PurchaseOrder.PurchaseOrderState) || c.PropertyName == nameof(PurchaseOrder.UploadDate)):
                            ScheduleThread.Instance.RunTask(App<Settings>.Instance.Schedules.SingleOrDefault(c => c.Class == typeof(PurchaseOrderUpload).FullName));
                            break;
                    }
                }
                catch (Exception e)
                {
                    LogErrorAsync(e).Wait();
                }
            });
            _isPrimed = true;
        }
    }
}
