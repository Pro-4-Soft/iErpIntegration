using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Infrastructure;

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

                var reference = Guid.NewGuid();
                try
                {
                    await ProcessAdjustments(clientAdjustments, reference, companySettings);
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
                    await Singleton<Web>.Instance.PostInvokeAsync("api/Audit/SetIntegrationReference", new
                    {
                        Ids = clientAdjustments.Select(c => c.Id).ToList(),
                        IntegrationReference = reference,
                        IntegrationMessage = e.Message
                    });
                }
            }
        }

        private async Task ProcessAdjustments(IEnumerable<Adjustment> adjustments, Guid reference, SiteSettings companySettings)
        {
            throw new Exception("FAIL TEST");
            //DO SOME WORK
        }
    }
}