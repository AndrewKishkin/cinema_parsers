using CinemasParser.Core.Abstract;
using CinemasParser.Core.Repository;
using CinemasParser.ServiceBus.Transfers;
using MassTransit;
using MessageContracts.Cinema;
using MessageContracts.Cinema.Models;
using MessageContracts.Parser;
using System;
using System.Threading.Tasks;
using Domain = Cinema.Domain;

namespace CinemasParser.ServiceBus.Consumers
{
    public class ParseConsumer : IConsumer<IParseCommand>
    {
        private readonly ICinemasParse _cinemasParse;
        private readonly IParserRepository _repository;
        private readonly ISendEndpointProvider _send;

        public ParseConsumer(ICinemasParse cinemasParse, IParserRepository repository, ISendEndpointProvider send)
        {
            _cinemasParse = cinemasParse;
            _repository = repository;
            _send = send;
        }

        public async Task Consume(ConsumeContext<IParseCommand> context)
        {
            var offset = context.Message.DateModified;
            var cinemaNetworkId = context.Message.CinemaNetworkId;

            var modifiedData = await _repository.GetModifiedData(offset);

            var sendEndpoint = _send.GetSendEndpoint(new Uri("queue:cinemas-data")).Result;

            await sendEndpoint.Send<IDataContract<ModifiedData<Domain.Cinema, Domain.Movie, Domain.Session>>>(new
            {
                CinemaNetworkId = cinemaNetworkId,
                Data = new ModifiedData<Domain.Cinema, Domain.Movie, Domain.Session>()
                {
                    Cinemas = modifiedData.Cinemas.ToDomainList(cinemaNetworkId),
                    Movies = modifiedData.Movies.ToDomainList(),
                    Sessions = modifiedData.Sessions.ToDomainList()
                }
            });;
        }
    }
}
