using Application.DTO.Activity;
using Application.DTO.Users;
using Application.DTO.Users.ResetPassword;
using Application.Exceptions;
using Application.Extensions;
using Application.Mapping;
using Application.UseCases;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : UserControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserEmailService _userEmailService;

        public UsersController(
            IUserService userService,
            IUserEmailService userEmailService,
            ILogger<UsersController> logger) : base(logger)
        {
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));

            _userService = userService;
            _userEmailService = userEmailService;
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
            //_logger.LogInformation($"> UsersController.GetById: Id = {id}");

            var user = await _userService.GetByIdAsync(id, ct);
            if (user == null)
                return NotFound("User not found.");

            var dto = UserMapper.ToDto(user);
            return Ok(dto);
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
        public async Task<ActionResult<UserInfoDto>> GetLoggedUser(CancellationToken ct)
        {
            //_logger.LogInformation($"> UsersController.GetLoggedUser");
            var result = await GetCurrentUserAsync(async userId =>
            {
                var user = await _userService.GetByIdAsync(userId, ct);
                if (user == null)
                    return null;
                else if (!user.Active)
                    throw new AccountNotActiveException("Account is currently inactive. Please contact support.");
                else
                    return UserMapper.ToDto(user);
            });

            if (result == null)
                return NotFound("User not found.");
            return
                Ok(result);
        }

        /// <summary>
        /// Sets the Level of the currently logged-in user
        /// </summary>
        /// <remarks>
        /// PATCH: api/users/level
        /// Requires authentication.
        /// </remarks>
        [HttpPatch("level")]
        [Authorize]
        public async Task<ActionResult<int>> SetLevel(
            [FromBody] LevelRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.SetLevel {request}");

            var result = await GetCurrentUserAsync(async userId =>
                await _userService.SetLevel(userId, request.Level, ct));

            return Ok(result);
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
        public async Task<ActionResult<ProgressResponseDto>> GetProgress(CancellationToken ct)
        {
            _logger.LogInformation($"> UsersController.GetProgress");
            var result = await GetCurrentUserAsync(async userId =>
                await _userService.GetProgress(userId, ct));
            return Ok(result);
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
        public async Task<ActionResult<int>> SaveProgress(
            [FromBody] ActivityProgressRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            var result = await GetCurrentUserAsync(async userId =>
            {
                _logger.LogInformation($"> UsersController.SaveProgress: userId={userId}, request={request}");
                return await _userService.SaveProgress(userId, request, ct);
            });

            return Ok(result);
        }

        #region Reset Password
        [HttpPost("password/request")]
        [EnableRateLimiting(SharedConstants.RateLimitResetPasswordRequestPolicy)]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> ResetPasswordRequest(
            [FromBody] ResetPasswordRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.ResetPasswordRequest: email={request.Email}");

            var user = await _userService.GetByEmailAsync(request.Email, ct);
            if (user == null)
                return Ok(true);

            var sendLinkDto = await _userService.GenerateResetPasswordRequestLink(user, ct);
            await _userEmailService.SendResetPasswordLink(sendLinkDto, ct);

            return Ok(true);
        }

        [HttpPost("password/reset")]
        [EnableRateLimiting(SharedConstants.RateLimitResetPasswordPolicy)]
        [AllowAnonymous]
        public async Task<ActionResult<bool>> ResetPassword(
            [FromBody] NewPasswordRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"> UsersController.ResetPassword: email={request.Email}");

            var result = await _userService.NewPasswordAsync(request, ct);
            return Ok(result);
        }
        #endregion

        #region Profile
        [HttpPatch("username")]
        [EnableRateLimiting(SharedConstants.RateLimitUpdateUsernamePolicy)]
        [Authorize]
        public async Task<ActionResult<bool>> UpdateUsername(
            [FromBody] UpdateUsernameDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            var result = await GetCurrentUserAsync(async userId =>
            {
                _logger.LogInformation($"> UsersController.UpdateUsername: userId={userId}, request={request}");
                var user = await _userService.UpdateUsernameAsync(request, userId, ct);
                if (user != null)
                {
                    await _userEmailService.SendUsernameChanged(user, request.NewUsername, ct);
                    return true;
                }
                return false;
            });
            return Ok(result);
        }

        [HttpPatch("password")]
        [EnableRateLimiting(SharedConstants.RateLimitUpdatePasswordPolicy)]
        [Authorize]
        public async Task<ActionResult<bool>> UpdatePassword(
            [FromBody] UpdatePasswordDto request,
            [FromServices] IValidator<UpdatePasswordDto> validator,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            await validator.ValidationCheck(request);

            var result = await GetCurrentUserAsync(async userId =>
            {
                _logger.LogInformation($"> UsersController.UpdatePassword: userId={userId}");
                var user = await _userService.UpdatePasswordAsync(request, userId, ct);
                if (user != null)
                {
                    await _userEmailService.SendPasswordChanged(user, ct);
                    return true;
                }
                return false;
            });
            return Ok(result);
        }

        [HttpPatch("delete")]
        [EnableRateLimiting(SharedConstants.RateLimitDeleteProfilePolicy)]
        [Authorize]
        public async Task<ActionResult<int>> DeleteProfile(
            [FromBody] DeleteProfileDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            var result = await GetCurrentUserAsync(async userId =>
            {
                _logger.LogInformation($"> UsersController.DeleteProfile: userId={userId}");
                return await _userService.DeleteProfileAsync(request, userId, ct);
            });
            return Ok(result);
        }
        #endregion
    }
}