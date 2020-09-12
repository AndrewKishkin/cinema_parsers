using System;
using System.Threading.Tasks;

namespace CinemasParser.Core.WebClient
{
    public interface IHttpService
    {
        Task<RequestResult> Execute(Action<RequestOptions> options);
    }
}
