using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;

namespace Pro4Soft.iErpIntegration.Workers.Upload
{
    public class WorkOrderUpload : BaseWorker
    {
        public WorkOrderUpload(ScheduleSetting settings) : base(settings)
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
                    var url = $@"odata/WorkOrder?
$select=Id,WorkOrderNumber,ReferenceNumber,ProducedQuantity,WarehouseId
&$expand=Product,
        ProductDetails,
        Warehouse($select=Id,WarehouseCode),
        Lines($orderby=LineNumber;$expand=Product,ConsumedProductDetails)
&$orderby=WorkOrderNumber
&$filter=UploadDate eq null and WorkOrderState eq 'Closed' and {(string.IsNullOrWhiteSpace(site.ClientName) ?
                        "ClientId eq null" :
                        $"Client/Name eq '{site.ClientName}'")}";
                    var wos = await Singleton<Web>.Instance.GetInvokeAsync<List<WorkOrder>>(url);
                    if (!wos.Any())
                        continue;

                    foreach (var wo in wos)
                    {
                        try
                        {
                            throw new Exception("FAIL TEST");
                            //DO SOME WORK

                            await Singleton<Web>.Instance.PostInvokeAsync("api/WorkOrderApi/CreateOrUpdate", new
                            {
                                wo.Id,
                                WarehouseId = wo.WarehouseId,
                                UploadDate = DateTime.UtcNow,
                                UploadedSuceeded = true,
                                UploadMessage = (string)null,
                            });

                            await LogAsync($"WO: [{wo.WorkOrderNumber}] for [{site.ClientName ?? site.Name}] uploaded");
                        }
                        catch (Exception e)
                        {
                            await Singleton<Web>.Instance.PostInvokeAsync("api/WorkOrderApi/CreateOrUpdate", new
                            {
                                wo.Id,
                                wo.WarehouseId,
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
                        case nameof(WorkOrder) when changeEvent.Changes.Any(c => c.PropertyName == nameof(WorkOrder.WorkOrderState) || c.PropertyName == nameof(WorkOrder.UploadDate)):
                            ScheduleThread.Instance.RunTask(App<Settings>.Instance.Schedules.SingleOrDefault(c => c.Class == typeof(WorkOrderUpload).FullName));
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
