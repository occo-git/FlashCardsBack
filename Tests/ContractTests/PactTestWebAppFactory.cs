using Application.DTO.Tokens;
using Docker.DotNet.Models;
using Infrastructure.DataContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;
using System.Text.Json;
using Tests.ContractTests.ProviderState;
using Xunit;

namespace Tests.ContractTests
{
    public class PactTestWebAppFactory : BaseTestWebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.UseKestrel()
                   .UseUrls(ProviderStates.BaseUrl)
                   .UseEnvironment("Development");

            builder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    // Process provider states by middleware
                    endpoints.MapPost(ProviderStates.ProviderStatesApi, async context =>
                    {
                        Console.WriteLine($"--------------------> MapPost");
                        var request = await JsonSerializer.DeserializeAsync<ProviderStateRequest>(context.Request.Body);

                        if (request?.State != null)
                        {
                            Console.WriteLine($"--------------------> Setting up provider state: {request.State}");
                            //var providerStateHandler = context.RequestServices.GetRequiredService<IProviderStateHandler>();
                            //await providerStateHandler.HandleProviderStateAsync(request.State);
                            context.Response.StatusCode = 200;
                            await context.Response.CompleteAsync();
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                        }
                    });
                });
            });
        }

        //protected override void ConfigureTestServices(IServiceCollection services)
        //{
        //    base.ConfigureTestServices(services);

        //    //services.AddScoped<HttpHelper>(sp =>
        //    //{
        //    //    var client = CreateClient();
        //    //    return new HttpHelper(client);
        //    //});

        //    services.AddScoped<DbHelper>(sp =>
        //    {
        //        var dbContext = sp.GetRequiredService<DataContext>();
        //        return new DbHelper(dbContext);
        //    });

        //    //services.AddScoped<IProviderStateHandler, ProviderStateHandler>();
        //}
    }
}