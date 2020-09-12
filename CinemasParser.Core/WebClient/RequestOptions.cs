using System;
using System.Collections.Concurrent;
using System.Net.Http;

namespace CinemasParser.Core.WebClient
{
    public class RequestOptions
    {
        public RequestOptions()
        {
            Headers = new ConcurrentDictionary<string, string>();
        }

        public Uri Host { get; set; }
        public string Path { get; set; }
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public ConcurrentDictionary<string, string> Parameters { get; set; }
        public ConcurrentDictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
    }
}
