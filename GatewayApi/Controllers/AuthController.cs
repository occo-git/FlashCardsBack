using Application.Abstractions.Services;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Extensions;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Auth;
using Shared.Configuration;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : UserControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly AuthOptions _authOptions;

        public AuthController(
            IAuthenticationService authenticationService,
            IOptions<AuthOptions> authOptions,
            ILogger<UsersController> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(authenticationService, nameof(authenticationService));
            ArgumentNullException.ThrowIfNull(authOptions, nameof(authOptions));
            ArgumentNullException.ThrowIfNull(authOptions.Value, nameof(authOptions.Value));

            _authenticationService = authenticationService;
            _authOptions = authOptions.Value;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <remarks>
        /// POST: api/auth/register
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="user">The user registration details.</param>
        /// <returns>
        /// The created user information.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(SharedConstants.RateLimitAuthPolicy)]
        public async Task<ActionResult<UserInfoDto>> Register(
            [FromBody] RegisterRequestDto request,
            [FromServices] IValidator<RegisterRequestDto> validator,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> AuthController.Register Username = {request.Username}");

            await validator.ValidationCheck(request);
            User createdUser = await _authenticationService.RegisterAsync(request, ct);
            var dto = UserMapper.ToDto(createdUser);

            return CreatedAtAction(
                 actionName: nameof(UsersController.GetById),
                 controllerName: "Users",
                 routeValues: new { id = dto.Id },
                 value: dto);
        }

        /// <summary>
        /// Logs in a user and returns a JWT token.
        /// Updates refresh token to a new one.
        /// </summary>
        /// <remarks>
        /// POST: api/auth/token
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="request">The user token details.</param>
        /// <returns>
        /// JWT tokens for the authenticated user.
        /// </returns>
        [HttpPost("token")]
        [AllowAnonymous]
        [EnableRateLimiting(SharedConstants.RateLimitAuthPolicy)]
        public async Task<ActionResult<TokenResponseDto>> Token(
            [FromBody] TokenRequestDto request,
            [FromServices] IValidator<TokenRequestDto> validator,
            CancellationToken ct)
        {
            _logger.LogInformation($"> AuthController.Token: {request.GrantType}");
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            if (!Clients.All.TryGetValue(request.ClientId, out var allowedGrants))
                return BadRequest("Invalid client");
            if (!allowedGrants.Contains(request.GrantType))
                return BadRequest("Unsupported grant type");

            var sessionId = GetSessionIdFromHeader();
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));

            return request.GrantType switch
            {
                GrantTypes.GrantTypePassword => await LoginAsync(request, validator, sessionId, ct),
                GrantTypes.GrantTypeGoogle => await GoogleLoginAsync(request, sessionId, ct),
                GrantTypes.GrantTypeRefreshToken => await RefreshAsync(request, sessionId, ct),
                _ => BadRequest("Invalid grant type")
            };
        }

        private async Task<ActionResult<TokenResponseDto>> LoginAsync(
            TokenRequestDto request, 
            IValidator<TokenRequestDto> validator, 
            string sessionId, 
            CancellationToken ct)
        {
            await validator.ValidationCheck(request);
            var tokenResponse = await _authenticationService.AuthenticateAsync(request, sessionId, ct);
            return Ok(tokenResponse);
        }

        private async Task<ActionResult<TokenResponseDto>> GoogleLoginAsync(
            TokenRequestDto request, 
            string sessionId, 
            CancellationToken ct)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.IdToken, nameof(request.IdToken));

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _authOptions.GoogleClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            if (payload == null)
                throw new UnauthorizedAccessException("Invalid Google token");

            if (!payload.EmailVerified)
                return BadRequest("Email not verified by Google");

            var tokenResponse = await _authenticationService.AuthenticateGoogleUserAsync(payload.Email, request.ClientId, sessionId, ct);
            return Ok(tokenResponse);
        }

        private async Task<ActionResult<TokenResponseDto>> RefreshAsync(
            TokenRequestDto request, 
            string sessionId, 
            CancellationToken ct)
        {
            var tokenResponse = await _authenticationService.UpdateTokensAsync(request, sessionId, ct);
            return Ok(tokenResponse);
        }

        /// <summary>
        /// Logouts the currently logged-in user
        /// </summary>
        /// <remarks>
        /// POST: api/auth/logout
        /// Requires authentication.
        /// </remarks>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<int>> Logout(CancellationToken ct)
        {
            _logger.LogInformation($"> AuthController.Logout");

            var result = await GetCurrentUserAsync(async userId =>
            {
                var sessionId = GetSessionIdFromHeader();
                ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
                return await _authenticationService.RevokeRefreshTokensAsync(userId, sessionId, ct);
            });

            return Ok(result);
        }

        private string GetSessionIdFromHeader() => Request.Headers[HeaderNames.SessionId].FirstOrDefault() ?? string.Empty;
    }
}
