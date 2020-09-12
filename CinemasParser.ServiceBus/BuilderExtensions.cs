using CinemasParser.ServiceBus.Consumers;
using MassTransit;
using MassTransit.Core;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CinemasParser.ServiceBus
{
    public static class BuilderExtensions
    {
        public static void AddServiceBus(this IServiceCollection services, BusSettings busSettings, string name)
        {
            services.AddMassTransit(x =>
            {
                x.AddConsumer<ParseConsumer>();

                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    IRabbitMqHost host = cfg.Host(new Uri(busSettings.Host), hostConfig =>
                    {
                        hostConfig.Username(busSettings.Username);
                        hostConfig.Password(busSettings.Password);
                    });

                    cfg.ReceiveEndpoint($"{name}-parser", e =>
                    {
                        e.ConfigureConsumer<ParseConsumer>(provider);
                    });
                }));
            });

            services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IHostedService, BusService>();
        }
    }
}
