using CinemasParser.Models;
using System.Threading.Tasks;

namespace CinemasParser.Core.Abstract
{
    public interface ICinemasParse
    {
        Task<ParseResult<Data>> ExecuteAsync();
    }
}
