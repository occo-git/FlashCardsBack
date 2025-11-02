using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Extensions;
using Application.Mapping;
using Application.Services;
using Application.Services.Contracts;
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
            if (accessTokenOptions == null || accessTokenOptions.Value == null)
                throw new ArgumentNullException(nameof(accessTokenOptions));
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
            [FromBody] RegisterRequestDto user,
            [FromServices] IValidator<RegisterRequestDto> validator,
            CancellationToken ct)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null");

            await validator.ValidationCheck(user);

            _logger.LogInformation($"> UsersController.Register: Username = {user.Username}");
            User newUser = UserMapper.ToDomain(user);
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
        /// <param name="user">The user login details.</param>
        /// <returns>
        /// JWT tokens for the authenticated user.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> Login(
            [FromBody] LoginRequestDto user,
            [FromServices] IValidator<LoginRequestDto> validator,
            CancellationToken ct)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null");

            var sessionId = GetSessionIdFromHeader();
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID is required.");

            _logger.LogInformation($"> UsersController.Login: sessionId = {sessionId}");

            await validator.ValidationCheck(user);

            _logger.LogInformation($"> UsersController.Login: Authenticate UserName = {user.Username}");
            var tokenResponse = await _authenticationService.AuthenticateAsync(user, sessionId, ct);

            _logger.LogInformation($"> UsersController.Login: Authenticated Username={user.Username}");
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
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null");

            var sessionId = GetSessionIdFromHeader();
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
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Request cannot be null");

            return await GetCurrentUser<bool>(token, async (ct, userId) =>
            {
                await _userService.SetLevel(userId, request.Level, ct);
                return Ok(true);
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
