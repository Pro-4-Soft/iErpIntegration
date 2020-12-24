using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;

namespace Pro4Soft.iErpIntegration.Workers.Upload
{
    public class AdjustmentUpload : BaseWorker
    {
        public AdjustmentUpload(ScheduleSetting settings) : base(settings)
        {
        }

        public override void Execute()
        {
            ExecuteAsync().Wait();
        }

        private async Task ExecuteAsync()
        {
            var allAdjustments = await Singleton<Web>.Instance.PostInvokeAsync<List<Adjustment>>("api/Audit/Execute", new
            {
                Filter = new
                {
                    Condition = "And",
                    Rules = new List<dynamic>
                    {
                        new
                        {
                            Field = "IntegrationReference",
                            Operator = "IsNull"
                        },
                        new
                        {
                            Condition = "Or",
                            Rules = new List<dynamic>
                            {
                                new
                                {
                                    Field = "SubType",
                                    Value = SubTypeConstants.AdjustIn,
                                    Operator = "Equal"
                                },
                                new
                                {
                                    Field = "SubType",
                                    Value = SubTypeConstants.AdjustOut,
                                    Operator = "Equal"
                                }
                            }
                        }
                    }
                }
            });
            if (!allAdjustments.Any())
                return;
            foreach (var companySettings in App<Settings>.Instance.Sites)
            {
                var clientAdjustments = allAdjustments.ToList();
                if (!string.IsNullOrWhiteSpace(companySettings.ClientName))
                    clientAdjustments = allAdjustments.Where(c => c.Client == companySettings.ClientName).ToList();

                clientAdjustments.Where(c => c.SubType == SubTypeConstants.AdjustOut).ToList().ForEach(c => c.Quantity *= -1);

                var reference = Guid.NewGuid();
                try
                {
                    //Inbound
                    await ProcessAdjustments(clientAdjustments.Where(c=>c.Quantity > 0), reference, companySettings);

                    //Outbound
                    await ProcessAdjustments(clientAdjustments.Where(c => c.Quantity > 0), reference, companySettings);

                    //Confirm success
                    await Singleton<Web>.Instance.PostInvokeAsync("api/Audit/SetIntegrationReference", new
                    {
                        Ids = clientAdjustments.Select(c => c.Id).ToList(),
                        IntegrationReference = reference,
                        IntegrationMessage = (string) null
                    });
                }
                catch (Exception e)
                {
                    await LogErrorAsync(e);

                    //Mark as failed
                    await Singleton<Web>.Instance.PostInvokeAsync("api/Audit/SetIntegrationReference", new
                    {
                        Ids = clientAdjustments.Select(c => c.Id).ToList(),
                        IntegrationReference = reference,
                        IntegrationMessage = e.Message
                    });
                }
            }
        }

        private async Task ProcessAdjustments(IEnumerable<Adjustment> adjustments, Guid reference, SiteSettings site)
        {
            //Inbound
            await site.WebInvokeAsync<dynamic>("IERPOperatSrv_EntradasComp/AddEntradaAsync", null, Method.POST, new
            {
                EP_Id_Empresa = site.ErpClientId,
                //SQ_ID_SalidasTipo = 4,//??
                SL_Referencia = reference,
                //MO_ID_Moneda = 19,//??
                //MF_Factor_Venta = 1,//??
                SL_Fecha_Emision = DateTime.UtcNow,
                //SL_ID_Estatus = 2,//??
                Detalles = adjustments.Where(c=>c.Quantity > 0).Select(c => new
                {
                    //PR_Id_Producto = c.Sku,//??
                    AL_Id_Almacen = site.WarehouseCode.ParseInt(),
                    //ME_Id_Medida = 25,//??
                    ED_Cantidad = c.Quantity,//Can be negative in case of a negative adjustment
                    //ED_Costo_Unitario = 50//??
                }).ToList()
            });
        }
    }
}