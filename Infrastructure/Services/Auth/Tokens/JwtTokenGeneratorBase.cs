using Application.Exceptions;
using Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth.Tokens
{
    public abstract class JwtTokenGeneratorBase
    {
        protected readonly SymmetricSecurityKey _sKey;

        protected JwtTokenGeneratorBase(SymmetricSecurityKey sKey)
        {
            ArgumentNullException.ThrowIfNull(sKey, nameof(sKey));
            _sKey = sKey;
        }

        protected Claim[] CreateClaims(User user, DateTime expires, string? sessionId = null)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new TokenInvalidFormatException("User's data is incomplete.");

            return new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Expiration, expires.ToString("o")), // "o" (ISO 8601)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
        }

        protected IEnumerable<Claim> GetClaims(string token)
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

        public Guid GetUserId(string token)
        {
            var claims = GetClaims(token);
            string? userIdStr = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (String.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                throw new TokenInvalidFormatException("Invalid or malformed confirmation token.");

            return userId;
        }

        public DateTime GetExpiration(string token)
        {
            var claims = GetClaims(token);
            string? expirationStr = claims.FirstOrDefault(c => c.Type == ClaimTypes.Expiration)?.Value;

            if (String.IsNullOrEmpty(expirationStr) || 
                !DateTime.TryParse(expirationStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiration)) // DateTimeStyles.RoundtripKind to parse "o" (ISO 8601)
                throw new TokenInvalidFormatException("Invalid or malformed confirmation token.");
            
            return expiration.ToUniversalTime();
        }

        public bool IsTokenExpired(string token)
        {
            var expiration = GetExpiration(token);
            return DateTime.UtcNow > expiration;
        }

        protected string GenerateJwtToken(Claim[] claims, DateTime expires)
        {
            var creds = new SigningCredentials(_sKey, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
