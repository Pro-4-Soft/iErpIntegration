using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using RestSharp;

namespace Pro4Soft.iErpIntegration.Infrastructure
{
    public class Web
    {
        private RestClient _client;
        private RestClient Client => _client ??= new RestClient(Singleton<EntryPoint>.Instance.CloudUrl)
        {
            Timeout = int.MaxValue
        };

        //Sync
        public T GetInvoke<T>(string url, string root = null) where T : class
        {
            if (url.ToLower().StartsWith("/odata") || url.ToLower().StartsWith("odata"))
                root = "value";
            var content = GetInvoke(url);
            return Utils.DeserializeFromJson<T>(content, root);
        }

        public string GetInvoke(string url)
        {
            return WebInvoke(url).Content;
        }

        public T PostInvoke<T>(string url, object payload) where T : class
        {
            return Utils.DeserializeFromJson<T>(PostInvoke(url, payload));
        }

        public string PostInvoke(string url, object payload)
        {
            return WebInvoke(url, Method.POST, payload).Content;
        }

        public IRestResponse WebInvoke(string url, Method method = Method.GET, object payload = null)
        {
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

            if (!string.IsNullOrWhiteSpace(Singleton<EntryPoint>.Instance.ApiKey))
                request.AddHeader("ApiKey", Singleton<EntryPoint>.Instance.ApiKey);

            if (payload != null)
                request.AddJsonBody(payload);

            var resp = Client.Execute(request);

            if (!resp.IsSuccessful)
                throw new BusinessWebException(resp.StatusCode, resp.Content);
            return resp;
        }

        public IRestResponse UploadStream(string url, Stream source)
        {
            var request = new RestRequest(url, Method.POST, DataFormat.None);
            if (!string.IsNullOrWhiteSpace(Singleton<EntryPoint>.Instance.ApiKey))
                request.AddHeader("ApiKey", Singleton<EntryPoint>.Instance.ApiKey);
            request.Files.Add(new FileParameter
            {
                Name = "file",
                Writer = destination =>
                {
                    source.CopyTo(destination);
                    source.Dispose();
                },
                FileName = "photo.png",
                ContentType = "image/png",
                ContentLength = source.Length
            });

            var result = Client.Execute(request);
            if (!result.IsSuccessful)
                throw new BusinessWebException(result.StatusCode, result.Content);
            return result;
        }

        //Async
        public async Task<T> GetInvokeAsync<T>(string url, string root = null) where T : class
        {
            if (url.ToLower().StartsWith("/odata") || url.ToLower().StartsWith("odata"))
                root = "value";
            var content = await GetInvokeAsync(url);
            return Utils.DeserializeFromJson<T>(content, root);
        }

        public async Task<string> GetInvokeAsync(string url)
        {
            return (await WebInvokeAsync(url)).Content;
        }

        public async Task<T> PostInvokeAsync<T>(string url, object payload) where T : class
        {
            return Utils.DeserializeFromJson<T>(await PostInvokeAsync(url, payload));
        }

        public async Task<string> PostInvokeAsync(string url, object payload)
        {
            return (await WebInvokeAsync(url, Method.POST, payload)).Content;
        }

        public async Task<IRestResponse> WebInvokeAsync(string url, Method method = Method.GET, object payload = null)
        {
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

            if (!string.IsNullOrWhiteSpace(Singleton<EntryPoint>.Instance.ApiKey))
                request.AddHeader("ApiKey", Singleton<EntryPoint>.Instance.ApiKey);

            if (payload != null)
                request.AddJsonBody(payload);

            var resp = await Client.ExecuteAsync(request);

            if (!resp.IsSuccessful)
                throw new BusinessWebException(resp.StatusCode, resp.Content);
            return resp;
        }

        public async Task<IRestResponse> UploadStreamAsync(string url, Stream source, string fileName = "photo.png", string contentType = "image/png")
        {
            var request = new RestRequest(url, Method.POST, DataFormat.None);
            if (!string.IsNullOrWhiteSpace(Singleton<EntryPoint>.Instance.ApiKey))
                request.AddHeader("ApiKey", Singleton<EntryPoint>.Instance.ApiKey);
            request.Files.Add(new FileParameter
            {
                Name = "file",
                Writer = destination =>
                {
                    source.CopyTo(destination);
                    source.Dispose();
                },
                FileName = fileName,
                ContentType = contentType,
                ContentLength = source.Length
            });

            var result = await Client.ExecuteAsync(request);
            if (!result.IsSuccessful)
                throw new BusinessWebException(result.StatusCode, result.Content);
            return result;
        }
    }
}
