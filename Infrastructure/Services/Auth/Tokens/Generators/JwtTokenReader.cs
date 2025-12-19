using Application.Abstractions.Services;
using Application.Exceptions;
using Shared.Auth;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth.Tokens.Generators
{
    public class JwtTokenReader : IJwtTokenReader
    {
        private IEnumerable<Claim> GetClaims(string token)
        {
            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                return jwt.Claims;
            }
            catch (Exception)
            {
                throw new TokenInvalidFormatException("Invalid or malformed confirmation token.");
            }
        }

        private string? GetClientId(IEnumerable<Claim> claims)
        {
            return claims.FirstOrDefault(c => c.Type == Clients.ClientIdClaim)?.Value;
        }

        private Guid GetUserId(IEnumerable<Claim> claims)
        {
            string? userIdStr = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new TokenInvalidFormatException("Invalid or malformed confirmation token.");

            return userId;
        }

        private DateTime GetExpiration(string token)
        {
            var claims = GetClaims(token);
            string? expirationStr = claims.FirstOrDefault(c => c.Type == ClaimTypes.Expiration)?.Value;

            if (string.IsNullOrEmpty(expirationStr) ||
                !DateTime.TryParse(expirationStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiration)) // DateTimeStyles.RoundtripKind to parse "o" (ISO 8601)
                throw new TokenInvalidFormatException("Invalid or malformed confirmation token.");

            return expiration.ToUniversalTime();
        }

        public bool IsTokenExpired(string token)
        {
            var expiration = GetExpiration(token);
            return DateTime.UtcNow > expiration;
        }

        public Guid GetUserIdWithCheck(string token, string grantType)
        {
            var claims = GetClaims(token);
            var clientId = GetClientId(claims);
            ArgumentNullException.ThrowIfNullOrEmpty(clientId, nameof(clientId));

            if (!Clients.All.TryGetValue(clientId, out var allowedGrants))
                throw new AppClientException("Invalid client.");
            if (!allowedGrants.Contains(grantType))
                throw new AppClientException("Unsupported grant type");

            return GetUserId(claims);
        }
    }        
}