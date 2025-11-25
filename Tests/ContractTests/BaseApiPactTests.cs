using Application.DTO.Tokens;
using Application.DTO.Users;
using FluentAssertions;
using Infrastructure.DataContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Tests.IntegrationTests;
using Tests.IntegrationTests.Api;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ContractTests
{
    [Trait("Category", "Contract")]
    public class BaseApiPactTests :
        IClassFixture<PactTestWebAppFactory>,
        IAsyncLifetime
    {
        protected readonly PactTestWebAppFactory _factory;
        protected readonly ITestOutputHelper _output;
        protected readonly HttpHelper _httpHelper;

        public BaseApiPactTests(PactTestWebAppFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;

            _output.WriteLine("---------------------> Creating HttpHelper in BaseApiPactTests");
            var client = _factory.CreateClient();
            _httpHelper = new HttpHelper(client);
            _output.WriteLine($"---------------------> HttpCllient.BaseAddress={_httpHelper.Client.BaseAddress}");
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _factory.Dispose();
            return Task.CompletedTask;
        }

        public static async Task CheckResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var body = string.IsNullOrWhiteSpace(content) ? "[empty]" : content;
                throw new Exception($"HTTP {response.StatusCode}: {body}");
            }
        }
    }
}