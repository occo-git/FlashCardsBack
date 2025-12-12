using Application.DTO.Tokens;
using Application.DTO.Users;
using FluentAssertions;
using Shared;
using Shared.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class HttpHelper
    {
        protected readonly HttpClient _client;

        public HttpHelper(HttpClient client) 
        {
            Console.WriteLine("==========> HttpHelper");
            _client = client;
        }

        public HttpClient Client => _client;

        public async Task<TokenResponseDto> LoginAsync(string username, string password)
        {
            Console.WriteLine($"--------------------------> LoginAsync: {username}");
            var sessionId = Guid.NewGuid();
            _client.DefaultRequestHeaders.Add(HeaderNames.SessionId, sessionId.ToString());

            var request = new TokenRequestDto(Clients.WebAppClientId, GrantTypes.GrantTypePassword, username, password);
            var response = await _client.PostAsJsonAsync("/api/auth/login", request);
            await CheckResponseAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            tokenResponse!.AccessToken.Should().NotBeNull();
            tokenResponse!.RefreshToken.Should().NotBeNull();
            tokenResponse!.SessionId.Should().NotBeNull();

            _client.DefaultRequestHeaders.Authorization = new(SharedConstants.TokenTypeBearer, tokenResponse.AccessToken);
            return tokenResponse;
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
