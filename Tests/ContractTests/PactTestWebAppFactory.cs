using Application.DTO.Tokens;
using Docker.DotNet.Models;
using Infrastructure.DataContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Shared;
using System;
using System.Net;
using System.Text;
using System.Text.Json;
using Tests.ContractTests.ProviderState;
using Xunit;

namespace Tests.ContractTests
{
    public class PactTestWebAppFactory : BaseTestWebAppFactory
    {
        private IProviderStateHandler? _providerStateHandler;
        public IProviderStateHandler TestProviderStateHandler
        {
            get
            {
                if (_providerStateHandler == null)
                {
                    var scope = this.Services.CreateScope();

                    var client = CreateClient();
                    client.BaseAddress = new Uri(ProviderStates.BaseUrl);
                    var httpHelper = new HttpHelper(client);

                    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                    var dbHelper = new DbHelper(dbContext);

                    _providerStateHandler = new ProviderStateHandler(dbHelper, httpHelper);
                }
                return _providerStateHandler;
            }
        }

        public HttpClient CreatePactClient()
        {
            var delegatingHandler = new ProviderStateDelegatingHandler(TestProviderStateHandler);
            return delegatingHandler.CreateClient();
        }
    }
}