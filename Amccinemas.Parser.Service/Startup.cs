using CinemasParser.Core.Abstract;
using CinemasParser.Core.WebClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Amccinemas.Parser.Service
{
    public class Startup
    {
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

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
