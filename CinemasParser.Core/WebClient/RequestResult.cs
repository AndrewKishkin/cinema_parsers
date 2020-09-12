using Newtonsoft.Json.Linq;
using System.Net;

namespace CinemasParser.Core.WebClient
{
    public enum ResultType
    {
        Error,
        Success,
        Forbidden,
        NotFound,
        Unauthorized,
        BadRequest,
        NotAccess,
        Timeout
    }

    public class RequestResult
    {
        public RequestResult()
        {
            Result = ResultType.Error;
        }

        public ResultType Result { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string Response { get; set; }

        public bool IsSuccess { get { return Result == ResultType.Success; } }
    }
}
