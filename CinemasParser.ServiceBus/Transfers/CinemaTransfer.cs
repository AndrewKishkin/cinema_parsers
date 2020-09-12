using System;
using System.Collections.Generic;
using System.Linq;
using Domain = Cinema.Domain;
using Parser = CinemasParser.Models;

namespace CinemasParser.ServiceBus.Transfers
{
    public static class CinemaTransfer
    {
        public static Domain.Cinema ToDomain(this Parser.Cinema cinema, Guid cinemaNetworkId)
        {
            return new Domain.Cinema()
            {
                CinemaNetworkId = cinemaNetworkId,
                ExternalId = cinema.ExternalId,
                Name = cinema.Name,
                Description = cinema.Description,
                Latitude = cinema.Latitude,
                Longitude = cinema.Longitude
            };
        }

        public static List<Domain.Cinema> ToDomainList(this List<Parser.Cinema> cinemas, Guid cinemaNetworkId)
        {
            return cinemas.Select(i => ToDomain(i, cinemaNetworkId)).ToList();
        }
    }
}
