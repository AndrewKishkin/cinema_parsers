using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voxcinemas.Parser.Service
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add http client
            services.AddHttpClient<IHttpService, HttpService>(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddTransient<ICinemasParse, Parser>();
            services.BuildServiceProvider().GetService<ICinemasParse>().ExecuteAsync();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
}
