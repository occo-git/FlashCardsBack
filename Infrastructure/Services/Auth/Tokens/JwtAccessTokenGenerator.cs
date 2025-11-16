using Application.Abstractions.Services;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth.Tokens
{
    public class JwtAccessTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<string>
    {
        private readonly int _accessTokenExpirationMinutes;

        public JwtAccessTokenGenerator(SymmetricSecurityKey sKey, IOptions<ApiTokenOptions> apiTokenOptions)
            : base(sKey)
        {
            ArgumentNullException.ThrowIfNull(apiTokenOptions, nameof(apiTokenOptions));
            ArgumentNullException.ThrowIfNull(apiTokenOptions.Value, nameof(apiTokenOptions.Value));

            _accessTokenExpirationMinutes = apiTokenOptions.Value.AccessTokenExpiresMinutes;
        }

        public string GenerateToken(User user, string? sessionId = null)
        {
            var expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);
            var claims = CreateClaims(user, expires, sessionId);
            return GenerateJwtToken(claims, expires);
        }
    }
}
