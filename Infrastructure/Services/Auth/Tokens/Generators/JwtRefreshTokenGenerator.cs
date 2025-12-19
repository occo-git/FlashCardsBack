using Application.Abstractions.Services;
using Domain.Entities;
using Domain.Entities.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth.Tokens.Generators
{
    public class JwtRefreshTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<RefreshToken>
    {
        private readonly int _refreshTokenExpirationDays;

        public JwtRefreshTokenGenerator(SymmetricSecurityKey sKey, IOptions<ApiTokenOptions> apiTokenOptions)
            : base(sKey)
        {
            ArgumentNullException.ThrowIfNull(apiTokenOptions, nameof(apiTokenOptions));
            ArgumentNullException.ThrowIfNull(apiTokenOptions.Value, nameof(apiTokenOptions.Value));

            _refreshTokenExpirationDays = apiTokenOptions.Value.RefreshTokenExpiresDays;
        }

        public RefreshToken GenerateToken(User user, string clientId, string? sessionId = null)
        {
            ArgumentNullException.ThrowIfNull(sessionId, nameof(sessionId));

            var expires = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
            var claims = CreateClaims(user, clientId, expires);
            var token = GenerateJwtToken(claims, expires);

            return new RefreshToken(user.Id, token, expires, sessionId);
        }

        public int ExpiresInSeconds { get { return _refreshTokenExpirationDays * 86400; } }
    }
}
