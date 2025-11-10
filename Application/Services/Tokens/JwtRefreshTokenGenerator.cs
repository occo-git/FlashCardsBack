using Application.Services.Contracts;
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

namespace Application.Services.Tokens
{
    public class JwtRefreshTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<RefreshToken>
    {
        private readonly int _refreshTokenExpirationDays;

        public JwtRefreshTokenGenerator(SymmetricSecurityKey sKey, IOptions<ApiTokenOptions> refreshTokenOptions)
            : base(sKey)
        {
            ArgumentNullException.ThrowIfNull(refreshTokenOptions, nameof(refreshTokenOptions));
            ArgumentNullException.ThrowIfNull(refreshTokenOptions.Value, nameof(refreshTokenOptions.Value));

            _refreshTokenExpirationDays = refreshTokenOptions.Value.RefreshTokenExpiresDays;
        }

        public RefreshToken GenerateToken(User user, string sessionId)
        {
            var expires = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays);
            var claims = GetClaims(user, expires, sessionId);
            var token = GenerateJwtToken(claims, expires);
            return new RefreshToken(token, user.Id, expires, sessionId);
        }
    }
}
