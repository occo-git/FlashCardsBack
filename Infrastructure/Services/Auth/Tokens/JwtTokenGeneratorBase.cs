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

        protected Claim[] GetClaims(User user, DateTime expires, string? sessionId = null)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new Exception("User's data is incomplete");

            return new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Expiration, expires.ToString("o")),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
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
