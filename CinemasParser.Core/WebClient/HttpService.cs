using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CinemasParser.Core.WebClient
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RequestResult> Execute(Action<RequestOptions> options)
        {
            RequestOptions opt = new RequestOptions();
            options.Invoke(opt);

            try
            {
                var uri = opt.Host != null ? opt.Host.AbsoluteUri.ToString() + opt.Path : _httpClient.BaseAddress + opt.Path;

                if(opt.Parameters != null)
                {
                    uri = new Uri(QueryHelpers.AddQueryString(uri, opt.Parameters)).ToString();
                }

                HttpRequestMessage request = new HttpRequestMessage(opt.Method, uri);

                foreach(var h in opt.Headers)
                {
                    request.Headers.Add(h.Key, h.Value);
                }

                if(opt.Method != HttpMethod.Get)
                {
                    if(!string.IsNullOrEmpty(opt.Body))
                    {
                        request.Content = new StringContent(opt.Body, Encoding.UTF8, "application/json");
                    }
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                return new RequestResult()
                {
                    Result = response.IsSuccessStatusCode ? ResultType.Success : ResultType.Error,
                    StatusCode = response.StatusCode,
                    Response = await response.Content.ReadAsStringAsync()
                };
            }
            catch(Exception e)
            {
                return new RequestResult()
                {
                    Response = e.Message
                };
            }
        }
    }
}
