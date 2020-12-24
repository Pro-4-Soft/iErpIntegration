using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;

namespace Pro4Soft.iErpIntegration.Workers.Upload
{
    public class SalesOrderUpload : BaseWorker
    {
        public SalesOrderUpload(ScheduleSetting settings) : base(settings)
        {
        }

        public override void Execute()
        {
            Subscribe();
            ExecuteAsync().Wait();
        }

        public async Task ExecuteAsync()
        {
            foreach (var companySettings in App<Settings>.Instance.Sites)
            {
                try
                {
                    var url = $@"odata/PickTicket?
$select=Id,PickTicketNumber,ReferenceNumber
&$orderby=PickTicketNumber
&$expand=Client($select=Name),
        Totes($select=Id,Sscc18Code,CartonNumber;
              $expand=Lines($select=Id,PickedQuantity;
                            $expand=Product($select=Id,Sku),
                                    PickTicketLine($select=Id,LineNumber,ReferenceNumber;$orderby=ReferenceNumber),
                                    LineDetails($select=Id,PacksizeEachCount,LotNumber,SerialNumber,ExpiryDate,PickedQuantity)))
&$filter=UploadDate eq null and PickTicketState eq 'Closed' and {(string.IsNullOrWhiteSpace(companySettings.ClientName) ?
                        "ClientId eq null" :
                        $"Client/Name eq '{companySettings.ClientName}'")}";

                    var sos = await Singleton<Web>.Instance.GetInvokeAsync<List<PickTicket>>(url);
                    if (!sos.Any())
                        continue;

                    foreach (var so in sos)
                    {
                        try
                        {
                            throw new Exception("FAIL TEST");
                            //DO SOME WORK

                            await Singleton<Web>.Instance.PostInvokeAsync("api/PickTicketApi/CreateOrUpdate", new
                            {
                                so.Id,
                                UploadDate = DateTime.UtcNow,
                                UploadedSuceeded = true,
                                UploadMessage = (string)null,
                            });

                            await LogAsync($"SO: [{so.PickTicketNumber}] for [{companySettings.ClientName ?? companySettings.Name}] uploaded");
                        }
                        catch (Exception e)
                        {
                            await Singleton<Web>.Instance.PostInvokeAsync("api/PickTicketApi/CreateOrUpdate", new
                            {
                                so.Id,
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
                        case nameof(PickTicket) when changeEvent.Changes.Any(c => c.PropertyName == nameof(PickTicket.PickTicketState) || c.PropertyName == nameof(PickTicket.UploadDate)):
                            ScheduleThread.Instance.RunTask(App<Settings>.Instance.Schedules.SingleOrDefault(c => c.Class == typeof(SalesOrderUpload).FullName));
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
