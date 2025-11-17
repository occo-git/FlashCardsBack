using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Exceptions;
using Application.Extensions;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using FluentValidation;
using Infrastructure.Services.RazorRenderer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System.Security.Claims;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : UserControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserEmailService _userEmailService;
        private readonly IAuthenticationService _authenticationService;

        public AuthController(
            IUserService userService,
            IUserEmailService userEmailService,
            IAuthenticationService authenticationService,
            ILogger<UsersController> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));
            ArgumentNullException.ThrowIfNull(authenticationService, nameof(authenticationService));

            _userService = userService;
            _userEmailService = userEmailService;
            _authenticationService = authenticationService;
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
        public async Task<UserInfoDto> Register(
            [FromBody] RegisterRequestDto request,
            [FromServices] IValidator<RegisterRequestDto> validator,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> AuthController.Register Username = {request.Username}");

            await validator.ValidationCheck(request);

            User newUser = UserMapper.ToDomain(request);
            var createdUser = await _userService.CreateNewAsync(newUser, ct);
            var send = await _userEmailService.SendEmailConfirmation(createdUser, ct);
            if (send.Success)
            {
                await _userService.AddAsync(createdUser, ct);
                var dto = UserMapper.ToDto(createdUser);
                return dto;
            }
            else
                throw new FailSendConfirmationException("Failed to send confirmation email.");
        }

        /// <summary>
        /// Logs in a user and returns a JWT token
        /// </summary>
        /// <remarks>
        /// POST: api/auth/login
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="request">The user login details.</param>
        /// <returns>
        /// JWT tokens for the authenticated user.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> Login(
            [FromBody] LoginRequestDto request,
            [FromServices] IValidator<LoginRequestDto> validator,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> AuthController.Login Username = {request.Username}");

            var sessionId = GetSessionIdFromHeader();
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
            _logger.LogInformation($"> AuthController.Login: sessionId = {sessionId}");

            await validator.ValidationCheck(request);

            _logger.LogInformation($"> AuthController.Login: Authenticate UserName = {request.Username}");
            var tokenResponse = await _authenticationService.AuthenticateAsync(request, sessionId, ct);

            _logger.LogInformation($"> AuthController.Login: Authenticated Username={request.Username}");
            return Ok(tokenResponse);
        }

        /// <summary>
        /// Updates refresh token to a new one
        /// </summary>
        /// <remarks>
        /// POST: api/auth/refresh
        /// Requires authentication.
        /// </remarks>
        /// <param name="request">The old refresh token.</param>
        /// <returns>
        /// New refresh token for the authenticated user.
        /// </returns>
        [HttpPost("refresh")]
        [Authorize]
        public async Task<ActionResult<TokenResponseDto>> Refresh(
            [FromBody] RefreshTokenRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation("> AuthController.Refresh");

            var sessionId = GetSessionIdFromHeader();
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
            _logger.LogInformation($"> AuthController.Refresh: sessionId = {sessionId}");

            var tokenResponse = await _authenticationService.UpdateTokensAsync(request.RefreshToken, sessionId, ct);
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
