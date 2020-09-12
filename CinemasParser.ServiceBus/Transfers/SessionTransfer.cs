using System.Collections.Generic;
using System.Linq;
using Domain = Cinema.Domain;
using Parser = CinemasParser.Models;

namespace CinemasParser.ServiceBus.Transfers
{
    public static class SessionTransfer
    {
        public static Domain.Session ToDomain(this Parser.Session cinema)
        {
            return new Domain.Session()
            {
                ExternalId = cinema.ExternalId,
                ExternalMovieId = cinema.MovieId,
                ExternalCinemaId = cinema.CinemaId,
                ShowTime = cinema.ShowTime,
                Url = cinema.Url
            };
        }

        public static List<Domain.Session> ToDomainList(this List<Parser.Session> cinemas)
        {
            return cinemas.Select(i => ToDomain(i)).ToList();
        }
    }
}
