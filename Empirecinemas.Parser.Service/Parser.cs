using CinemasParser.Core;
using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using CinemasParser.Models;
using System;
using System.Threading.Tasks;

namespace Empirecinemas.Parser.Service
{
    internal class Parser : ICinemasParse
    {
        private readonly IHttpService _http;

        public Parser(IHttpService http)
        {
            _http = http;
        }

        public Task<ParseResult<Data>> ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
