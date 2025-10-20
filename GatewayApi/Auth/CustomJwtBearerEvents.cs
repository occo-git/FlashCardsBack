using Infrastructure.Repositories.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GatewayApi.Auth
{
    public class CustomJwtBearerEvents : JwtBearerEvents
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public CustomJwtBearerEvents(IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository), "RefreshTokenRepository cannot be null.");
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            Console.WriteLine(">>> JwtEvents.TokenValidated called");

            var sessionId = context.HttpContext.Request.Headers[HeaderNames.SessionId].FirstOrDefault();
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine(">>> JwtEvents.TokenValidated: SessionId header is missing.");
                context.Fail("SessionId header missing.");
                return;
            }

            var userIdStr = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                Console.WriteLine(">>> JwtEvents.TokenValidated: UserId claim is invalid.");
                context.Fail("UserId claim is invalid.");
                return;
            }

            Console.WriteLine($">>> JwtEvents.TokenValidated: UserId = {userId}, (Header) sessionId = {sessionId}");
            var ct = context.HttpContext.RequestAborted;
            bool isValid = await _refreshTokenRepository.ValidateRefreshTokenAsync(userId, sessionId, ct);
            if (!isValid)
            {
                Console.WriteLine(">>> JwtEvents.TokenValidated: Invalid session.");
                context.Fail("Invalid session.");
                return;
            }
        }
    }
}
