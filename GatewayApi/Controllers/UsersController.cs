using Application.DTO.Tokens;
using Application.DTO.User;
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
        /// GET: api/users/info/{id}
        /// Requires authentication.
        /// </remarks>
        /// <returns>
        /// A user info.
        /// </returns>
        [HttpGet("info/{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetById(Guid id, CancellationToken ct)
        {
            _logger.LogInformation($"> UsersController.GetById: Id = {id}");

            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {id} not found.");

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
        /// <param name="registerUser">The user registration details.</param>
        /// <returns>
        /// The created user information.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserInfoDto>> Register(
            [FromBody] CreateUserDto user,
            [FromServices] IValidator<CreateUserDto> validator,
            CancellationToken ct)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user), "User cannot be null");

            await validator.ValidationCheck(user);

            _logger.LogInformation($"> UsersController.Register: Username = {user.Username}");
            User newUser = UserMapper.ToDomain(user);
            var createdUser = await _userService.CreateAsync(newUser, ct);

            // Returning 201 Created with a link to the newly created user
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Logs in a user and returns a JWT token
        /// </summary>
        /// <remarks>
        /// POST: api//users/login
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="loginUser">The user login details.</param>
        /// <returns>
        /// JWT tokens for the authenticated user.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> Login(
            [FromBody] LoginUserDto user,
            [FromServices] IValidator<LoginUserDto> validator,
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
            return await GetCurrentUser<UserInfoDto>(token, async (ct, ut) =>
            {
                _logger.LogInformation("> UsersController.GetLoggedUser: Finding user: Id={id}", ut.UserId);
                var user = await _userService.GetByIdAsync(ut.UserId, ct);
                if (user == null)
                {
                    _logger.LogWarning("> UsersController.GetLoggedUser: User not found: Id={id}", ut.UserId);
                    return NotFound("User not found");
                }
                else
                {
                    _logger.LogInformation("> UsersController.GetLoggedUser: Found user: Id={id}, Username={username}", user.Id, user.Username);
                    //_logger.LogInformation("Access token {accessToken}", accessToken);
                    var dto = UserMapper.ToDto(user, ut.AccessToken, ut.RefreshToken);
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
            return await GetCurrentUser<bool>(token, async (ct, ut) =>
            {
                var sessionId = GetSessionIdFromHeader();
                await _authenticationService.RevokeRefreshTokensAsync(ut.UserId, sessionId, ct);
                return Ok(true);
            });
        }

        private async Task<ActionResult<T>> GetCurrentUser<T>(
            CancellationToken ct,
            Func<CancellationToken, UserTokens, Task<ActionResult<T>>> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accessToken = GetAccessTokenFromHeader();
            var refreshToken = GetRefreshTokenFromHeader();
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("> UsersController.GetCurrentUser: User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                return await action(ct, new UserTokens(userId, accessToken, refreshToken));
            }
            else
            {
                _logger.LogError("> UsersController.GetCurrentUser: Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }

        private string GetSessionIdFromHeader() => Request.Headers[HeaderNames.SessionId].FirstOrDefault() ?? string.Empty;
        private string GetAccessTokenFromHeader() => Request.Headers[HeaderNames.AccessToken].FirstOrDefault() ?? string.Empty;
        private string GetRefreshTokenFromHeader() => Request.Headers[HeaderNames.RefreshToken].FirstOrDefault() ?? string.Empty;


        private record UserTokens(Guid UserId, string? AccessToken, string? RefreshToken);
    }
}
