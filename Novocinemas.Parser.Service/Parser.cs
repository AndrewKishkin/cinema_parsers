using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

namespace Novocinemas.Parser.Service
{
    internal class Parser : ICinemasParse
    {
        private readonly IHttpService _http;

        public Parser(IHttpService http)
        {
            _http = http;
        }

        public async Task<ParseResult<Data>> ExecuteAsync()
        {
            ConcurrentDictionary<string, string> headers = new ConcurrentDictionary<string, string>();
            headers.TryAdd("ContentType", "application/x-www-form-urlencoded");

            ConcurrentDictionary<string, string> parameters = new ConcurrentDictionary<string, string>();
            parameters.TryAdd("movieid", "");
            parameters.TryAdd("cinemaid", "");
            parameters.TryAdd("date", DateTime.Now.ToString("yyyy-MM-dd"));
            parameters.TryAdd("experience", "");

            var result = await _http.Execute(o =>
            {
                o.Host = new Uri("https://uae.novocinemas.com/showtimes/GetCinemaShowTimes");
                o.Method = HttpMethod.Get;
                o.Headers = headers;
                o.Parameters = parameters;
            });

            return new ParseResult<Data>(Result.Success);
        }
    }
}
