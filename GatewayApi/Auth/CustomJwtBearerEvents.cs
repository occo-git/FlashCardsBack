using Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Auth;
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
            ArgumentNullException.ThrowIfNull(refreshTokenRepository, nameof(refreshTokenRepository));
            _refreshTokenRepository = refreshTokenRepository;
        }

        public override async Task TokenValidated(TokenValidatedContext context)
        {
            // sessionId header
            var sessionId = context.HttpContext.Request.Headers[HeaderNames.SessionId].FirstOrDefault();
            if (string.IsNullOrEmpty(sessionId))
            {
                context.Fail("SessionId header missing.");
                return;
            }

            // client-id claim
            var clientId = context.Principal?.FindFirst(Clients.ClientIdClaim)?.Value;
            if (string.IsNullOrEmpty(clientId) || !Clients.All.ContainsKey(clientId))
            {
                context.Fail("ClientId claim is invalid.");
                return;
            }            
            
            // userId claim
            var userIdStr = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                context.Fail("UserId claim is invalid.");
                return;
            }

            var ct = context.HttpContext.RequestAborted;
            //Console.WriteLine($">>> JwtEvents.TokenValidated: request.Path = {context.Request.Path}");
            bool isValid = await _refreshTokenRepository.ValidateAsync(userId, sessionId, ct);
            if (!isValid)
            {
                context.Fail("Invalid session.");
                return;
            }
        }
    }
}