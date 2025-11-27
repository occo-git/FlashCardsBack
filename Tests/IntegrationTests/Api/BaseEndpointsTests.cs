using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using FluentAssertions;
using Shared;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.IntegrationTests.Api
{
    [Trait("Category", "Api")]
    public class BaseEndpointsTests : BaseIntegrationTest<IUserService>
    {
        protected readonly HttpClient _client;

        //public enum HttpMethods { Get , Post, Put, Delete, Patch }

        public BaseEndpointsTests(IntegrationTestWebAppFactory factory) : base(factory)
        { 
            _client = factory.CreateClient();
            Console.WriteLine($"--> BaseEndpointsTests: BaseAddress={_client.BaseAddress}");
        }

        protected async Task<TokenResponseDto> AuthorizeAsync(string username = "testuser", string email = "test@test.com", string password = "strongpass123!")
        {
            await AddConfirmedUserAsync(username, email, password);
            var tokenResponse = await LoginAsync(username, password);
            _client.DefaultRequestHeaders.Authorization = new("Bearer", tokenResponse.AccessToken);

            return tokenResponse;
        }

        private async Task<UserInfoDto> RegisterAsync(string username, string email, string password)
        {
            var request = new RegisterRequestDto(username, email, password);
            var response = await _client.PostAsJsonAsync("/api/auth/register", request);
            await CheckResponseAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var userInfo = await response.Content.ReadFromJsonAsync<UserInfoDto>();
            userInfo!.Username.Should().Be(username);

            return userInfo;
        }

        private async Task<TokenResponseDto> LoginAsync(string username, string password)
        {
            var sessionId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Add(HeaderNames.SessionId, sessionId.ToString());

            var request = new LoginRequestDto(username, password);
            var response = await _client.PostAsJsonAsync("/api/auth/login", request);
            await CheckResponseAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            tokenResponse!.AccessToken.Should().NotBeNull();
            tokenResponse!.RefreshToken.Should().NotBeNull();
            tokenResponse!.SessionId.Should().NotBeNull();

            return tokenResponse;
        }

        protected async Task CheckResponseAsync(HttpResponseMessage response)
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