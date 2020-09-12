using System.Collections.Generic;
using System.Linq;
using Domain = Cinema.Domain;
using Parser = CinemasParser.Models;

namespace CinemasParser.ServiceBus.Transfers
{
    public static class MovieTransfer
    {
        public static Domain.Movie ToDomain(this Parser.Movie cinema)
        {
            return new Domain.Movie()
            {
                ExternalId = cinema.ExternalId,
                Title = cinema.Title
            };
        }

        public static List<Domain.Movie> ToDomainList(this List<Parser.Movie> cinemas)
        {
            return cinemas.Select(i => ToDomain(i)).ToList();
        }
    }
}
