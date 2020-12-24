using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Pro4Soft.iErpIntegration.Infrastructure;
using RestSharp;
using RestSharp.Authenticators;

namespace Pro4Soft.iErpIntegration.Workers
{
    public class SiteSettings
    {
        public string Name { get; set; }
        public string ClientName { get; set; }
        public int ErpClientId { get; set; }
        public string WarehouseCode { get; set; }
        public string Url { get; set; }
        public string BearerToken { get; set; }

        public string PurchaseOrderStatusForDownload { get; set; }
        public string SalesOrderStatusForDownload { get; set; }

        private RestClient _client;
        public async Task<T> WebInvokeAsync<T>(string url, string root, Method method = Method.GET, object payload = null) where T : class
        {
            _client ??= new RestClient($"{Url}/IERPOperatSrv/api")
            {
                Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(BearerToken, "Bearer")
            };

            var split = url.Split('?');
            if (split.Length > 1)
                url = split[0];
            var request = new RestRequest(url, method) { RequestFormat = DataFormat.Json };
            if (split.Length > 1)
            {
                var qryParsString = split.Last();
                var parsed = HttpUtility.ParseQueryString(qryParsString);
                foreach (var par in parsed.Cast<string>().Where(c => !string.IsNullOrWhiteSpace(c)))
                    request.AddQueryParameter(par, parsed.Get(par));
            }

            if (method == Method.POST || method == Method.PATCH || method == Method.PUT)
                request.AddJsonBody(payload);

            var result = await _client.ExecuteAsync(request);

            if (result.IsSuccessful)
                return Utils.DeserializeFromJson<T>(result.Content, root);
            throw new BusinessWebException(result.StatusCode, result.Content);
        }
    }

    public class Settings : AppSettings
    {
        public List<SiteSettings> Sites { get; set; } = new List<SiteSettings>();

        public List<ScheduleSetting> Schedules { get; set; } = new List<ScheduleSetting>();
        public (TimeSpan, List<ScheduleSetting>) NextTime(DateTimeOffset now)
        {
            var smallestTimespan = TimeSpan.MaxValue;
            var schedules = new List<ScheduleSetting>();
            foreach (var schedule in Schedules
                .Where(c => c.Start != null)
                .Where(c => c.Sleep != null)
                .Where(c => c.Active))
            {
                var pollTimeout = schedule.GetTimeout(now);
                if (smallestTimespan == pollTimeout)
                    schedules.Add(schedule);
                else if (smallestTimespan > pollTimeout)
                {
                    smallestTimespan = pollTimeout;
                    schedules = new List<ScheduleSetting> { schedule };
                }
            }
            return (smallestTimespan, schedules);
        }
    }

    public class SiteCache
    {
        public string Name { get; set; }
        public DateTime? LastProductDownload { get; set; }
        public DateTime? LastPurchaseOrderDownload { get; set; }
        public DateTime? LastSalesOrderDownload { get; set; }
    }
}
