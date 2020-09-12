using Newtonsoft.Json;

namespace CinemasParser.Core
{
    public enum Result
    {
        Error,
        Success,
        NotAccess,
        NotFound
    }

    public interface IParseResult<T>
    {
        public Result Result { get; set; }

        public T Data { get; set; }
    }

    public class ParseResult<T> : IParseResult<T> where T : class
    {
        public ParseResult(Result result)
        {
            Result = result;
        }

        public Result Result { get; set; }

        public T Data { get; set; }

        [JsonIgnore]
        public bool IsSuccess { get { return Result == Result.Success; } }
    }
}
