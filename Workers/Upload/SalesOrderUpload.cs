using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

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
            foreach (var site in App<Settings>.Instance.Sites)
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
&$filter=UploadDate eq null and PickTicketState eq 'Closed' and {(string.IsNullOrWhiteSpace(site.ClientName) ?
                        "ClientId eq null" :
                        $"Client/Name eq '{site.ClientName}'")}";

                    var sos = await Singleton<Web>.Instance.GetInvokeAsync<List<PickTicket>>(url);
                    if (!sos.Any())
                        continue;

                    foreach (var so in sos)
                    {
                        try
                        {
                            await site.WebInvokeAsync<dynamic>("IERPOperatSrv_DespachosComp/AddDespachoAsync", null, Method.POST, new
                            {
                                //DI_Id_Direccion = 211,//??
                                //CN_Id_Contacto = 169,//??
                                EP_Id_Empresa = site.ErpClientId,
                                DS_Fecha_Despacho = DateTime.UtcNow,
                                DS_Fecha_Emision = DateTime.UtcNow,
                                DS_Referencia = so.PickTicketNumber,
                                //DSE_Id_Estatus = 1,//??
                                //EN_Id_Cliente = 331,//??
                                Detalles = so.GetOrderLines().Select(c => new
                                {
                                    PR_Id_Producto = c.Product.ReferenceNumber?.ParseInt(),//??
                                    //ME_Id_Medida = 25,//??
                                    AL_Id_Almacen = site.WarehouseCode.ParseInt(),
                                    ED_Cantidad = c.PickedQuantity,
                                }).ToList()
                            });

                            //Confirm success
                            await Singleton<Web>.Instance.PostInvokeAsync("api/PickTicketApi/CreateOrUpdate", new
                            {
                                so.Id,
                                UploadDate = DateTime.UtcNow,
                                UploadedSuceeded = true,
                                UploadMessage = (string)null,
                            });

                            await LogAsync($"SO: [{so.PickTicketNumber}] for [{site.ClientName ?? site.Name}] uploaded");
                        }
                        catch (Exception e)
                        {
                            //Mark as failed
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
