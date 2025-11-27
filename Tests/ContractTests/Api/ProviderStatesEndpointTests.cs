using Application.DTO.Users;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.ContractTests.ProviderState;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ContractTests.Api
{
    //[Trait("Category", "Contract")]
    public class ProviderStatesEndpointTests : BaseApiPactTests
    {
        public ProviderStatesEndpointTests(PactTestWebAppFactory factory, ITestOutputHelper output) 
            : base(factory, output)
        { }

        //[Fact]
        public async Task ProviderStatesEndpoint_ReturnsSuccess()
        {
            // Arrange
            var request = new ProviderStateRequest() { State = ProviderStates.UserIsAuthenticated };

            // Act
            var response = await PactHttpClient.PostAsJsonAsync(ProviderStates.ProviderStatesApi, request);
            await CheckResponseAsync(response);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}