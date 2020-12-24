using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pro4Soft.iErpIntegration.Dto.P4W;
using Pro4Soft.iErpIntegration.Workers;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public abstract class BaseWorker
    {
        public readonly ScheduleSetting Settings;

        protected BaseWorker(ScheduleSetting settings)
        {
            Settings = settings;
        }

        public abstract void Execute();

        public async Task LogAsync(string msg)
        {
            await Console.Out.WriteLineAsync(msg);
        }

        public async Task LogErrorAsync(Exception ex)
        {
            await LogErrorAsync(ex.ToString());
        }

        public async Task LogErrorAsync(string msg)
        {
            await Console.Error.WriteLineAsync(msg);
        }

        //P4W WMS
        private static readonly Lazy<List<Client>> ClientCache = new Lazy<List<Client>>(() => Singleton<Web>.Instance.GetInvokeAsync<List<Client>>("odata/Client?$select=Id,Name").Result);
        protected async Task<Guid?> GetClientIdAsync(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                return null;

            var clientId = ClientCache.Value.SingleOrDefault(c => c.Name == clientName)?.Id;
            if (clientId != null)
                return clientId;

            var client = (await Singleton<Web>.Instance.GetInvokeAsync<List<Client>>($"odata/Client?$filter=Name eq '{clientName}'&$select=Id,Name", "value")).SingleOrDefault();
            if (client == null)
                throw new BusinessWebException($"Client [{clientName}] is not setup");
            ClientCache.Value.Add(client);
            return client.Id;
        }

        protected async Task<Guid?> IdLookupAsync(string url, string filter)
        {
            var dataset = await Singleton<Web>.Instance.GetInvokeAsync<List<dynamic>>($"{url}?$select=Id&$filter={filter}", "value");
            if (!dataset.Any())
                return null;
            if (dataset.Count > 1)
                throw new BusinessWebException($"More than one record found");
            return Guid.TryParse(dataset.SingleOrDefault()?.Id.ToString(), out Guid id) ? id : throw new BusinessWebException($"Cannot parse response");
        }
    }
}
