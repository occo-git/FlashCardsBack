using Application.Abstractions.Services;
using Application.DTO.Activity;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Extensions;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System.Security.Claims;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly int _accessTokenMinutesBeforeExpiration = 3;
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IOptions<ApiTokenOptions> accessTokenOptions,
            IAuthenticationService authenticationService,
            IUserService userService,
            ILogger<UsersController> logger)
        {
            ArgumentNullException.ThrowIfNull(accessTokenOptions, nameof(accessTokenOptions));
            ArgumentNullException.ThrowIfNull(accessTokenOptions.Value, nameof(accessTokenOptions.Value));

            _accessTokenMinutesBeforeExpiration = accessTokenOptions.Value.AccessTokenMinutesBeforeExpiration;

            _userService = userService;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        /// <summary>        
        /// Gets user info
        /// </summary>
        /// <remarks>
        /// GET: api/users/{id}
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// A user info.
        /// </returns>
        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation($"> UsersController.GetById: Id = {id}");

            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound("User not found.");

            var dto = UserMapper.ToDto(user);
            return Ok(dto);
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <remarks>
        /// POST: api/users/register
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="user">The user registration details.</param>
        /// <returns>
        /// The created user information.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserInfoDto>> Register(
            [FromBody] RegisterRequestDto request,
            [FromServices] IValidator<RegisterRequestDto> validator,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.Register Username = {request.Username}");

            await validator.ValidationCheck(request);
            User newUser = UserMapper.ToDomain(request);
            var createdUser = await _userService.CreateAsync(newUser, ct);
            var dto = UserMapper.ToDto(createdUser);

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        /// <summary>
        /// Logs in a user and returns a JWT token
        /// </summary>
        /// <remarks>
        /// POST: api/users/login
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
            _logger.LogInformation($"> UsersController.Login Username = {request.Username}");

            var sessionId = GetSessionIdFromHeader();
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
            _logger.LogInformation($"> UsersController.Login: sessionId = {sessionId}");

            await validator.ValidationCheck(request);

            _logger.LogInformation($"> UsersController.Login: Authenticate UserName = {request.Username}");
            var tokenResponse = await _authenticationService.AuthenticateAsync(request, sessionId, ct);

            _logger.LogInformation($"> UsersController.Login: Authenticated Username={request.Username}");
            return Ok(tokenResponse);
        }

        /// <summary>
        /// Updates refresh token to a new one
        /// </summary>
        /// <remarks>
        /// POST: api/users/refresh
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
            _logger.LogInformation("> UsersController.Refresh");

            var sessionId = GetSessionIdFromHeader();
            ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
            _logger.LogInformation($"> UsersController.Refresh: sessionId = {sessionId}");

            var tokenResponse = await _authenticationService.UpdateTokensAsync(request.RefreshToken, sessionId, ct);
            return Ok(tokenResponse);
        }

        /// <summary>
        /// Gets the currently logged-in user information
        /// </summary>
        /// <remarks>
        /// GET: api/users/me
        /// Requires authentication.
        /// </remarks>
        /// <returns>The user information.</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetLoggedUser(CancellationToken token)
        {
            _logger.LogInformation($"> UsersController.GetLoggedUser");
            return await GetCurrentUser<UserInfoDto>(token, async (ct, userId) =>
            {
                _logger.LogInformation("> UsersController.GetLoggedUser: Finding user: Id={id}", userId);
                var user = await _userService.GetByIdAsync(userId, ct);
                if (user == null)
                {
                    _logger.LogWarning("> UsersController.GetLoggedUser: User not found: Id={id}", userId);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("> UsersController.GetLoggedUser: Found user: Id={id}, Username={username}", user.Id, user.Username);
                    //_logger.LogInformation("Access token {accessToken}", accessToken);
                    var dto = UserMapper.ToDto(user);
                    return Ok(dto);
                }
            });
        }

        /// <summary>
        /// Logouts the currently logged-in user
        /// </summary>
        /// <remarks>
        /// POST: api/users/logout
        /// Requires authentication.
        /// </remarks>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<bool>> Logout(CancellationToken token)
        {
            _logger.LogInformation($"> UsersController.Logout");
            return await GetCurrentUser<bool>(token, async (ct, userId) =>
            {
                var sessionId = GetSessionIdFromHeader();
                await _authenticationService.RevokeRefreshTokensAsync(userId, sessionId, ct);
                return Ok(true);
            });
        }

        /// <summary>
        /// Sets the Level of the currently logged-in user
        /// </summary>
        /// <remarks>
        /// POST: api/users/level
        /// Requires authentication.
        /// </remarks>
        [HttpPost("level")]
        [Authorize]
        public async Task<ActionResult<bool>> SetLevel(
            [FromBody] LevelRequestDto request,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.SetLevel {request}");

            return await GetCurrentUser<bool>(token, async (ct, userId) =>
            {
                var result = await _userService.SetLevel(userId, request.Level, ct);
                return Ok(result);
            });
        }

        /// <summary>
        /// Get progress of the currently logged-in user
        /// </summary>
        /// <remarks>
        /// GET: api/users/progress
        /// Requires authentication.
        /// </remarks>
        [HttpGet("progress")]
        [Authorize]
        public async Task<ActionResult<ProgressResponseDto>> GetPorgress(CancellationToken token)
        {
            _logger.LogInformation($"> UsersController.GetPorgress");
            return await GetCurrentUser<ProgressResponseDto>(token, async (ct, userId) =>
            {
                var progress = await _userService.GetProgress(userId, ct);
                return Ok(progress);
            });
        }

        /// <summary>
        /// Saves an activity progress for the currently logged-in user
        /// </summary>
        /// <remarks>
        /// POST: api/users/progress/save
        /// Requires authentication.
        /// </remarks>
        [HttpPost("progress/save")]
        [Authorize]
        public async Task<ActionResult<bool>> SaveProgress(
            [FromBody] ActivityProgressRequestDto request,
            CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.SaveProgress {request}");

            return await GetCurrentUser<bool>(token, async (ct, userId) =>
            {
                var result = await _userService.SaveProgress(userId, request, ct);
                return Ok(result);
            });
        }

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, Guid, Task<ActionResult<T>>> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("> UsersController.GetCurrentUser: User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                _logger.LogInformation($"> UsersController.GetCurrentUser UserId = {userId}");
                return await action(ct, userId);
            }
            else
            {
                _logger.LogError("> UsersController.GetCurrentUser: Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }

        private string GetSessionIdFromHeader() => Request.Headers[HeaderNames.SessionId].FirstOrDefault() ?? string.Empty;
    }
}
