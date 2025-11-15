using Application.Abstractions.Services;
using Application.DTO.Tokens;
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
    public class JwtConfirmationTokenGenerator : JwtTokenGeneratorBase, ITokenGenerator<ConfirmationTokenDto>
    {
        private readonly int _confirmationTokenExpirationMinutes;

        public JwtConfirmationTokenGenerator(SymmetricSecurityKey sKey, IOptions<ApiTokenOptions> apiTokenOptions)
            : base(sKey)
        {
            ArgumentNullException.ThrowIfNull(apiTokenOptions, nameof(apiTokenOptions));
            ArgumentNullException.ThrowIfNull(apiTokenOptions.Value, nameof(apiTokenOptions.Value));

            _confirmationTokenExpirationMinutes = apiTokenOptions.Value.ConfirmationTokenExpiresMinutes;
        }

        public ConfirmationTokenDto GenerateToken(User user, string? sessionId = null)
        {
            var expires = DateTime.UtcNow.AddMinutes(_confirmationTokenExpirationMinutes);
            var claims = GetClaims(user, expires, sessionId);
            var token = GenerateJwtToken(claims, expires);

            return new ConfirmationTokenDto(user.Id, token);
        }
    }
}
