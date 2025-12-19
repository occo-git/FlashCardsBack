using Application.DTO.Users.EmailConfirmation;
using Application.Exceptions;
using Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [Route("api/email")]
    public class EmailController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUserEmailService _userEmailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IUserService userService,
            IUserEmailService userEmailService,
            ILogger<EmailController> logger)
        {
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _userService = userService;
            _userEmailService = userEmailService;
            _logger = logger;
        }

        /// <summary>
        /// Resend email confirmation of a user
        /// </summary>
        /// <remarks>
        /// POST: api/email/resend-email-confirmation
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="token">Confirmation token.</param>
        /// <returns>
        /// The result of the email confirmation.
        /// </returns>
        [HttpPost("resend-email-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ReSendEmailConfirmation(
            [FromBody] SendEmailConfirmationRequestDto request,
            CancellationToken ct)
        {
            _logger.LogInformation($"> EmailController.ReSendEmailConfirmation");

            var user = await _userService.GetByEmailAsync(request.Email, ct);
            if (user == null)
                return Ok();
            if (user.EmailConfirmed)
            { 
                await _userEmailService.SendGreeting(user, ct);
                return Ok();
            }

            var sendLinkDto = await _userService.GenerateEmailConfirmationLinkAsync(user, ct);
            await _userService.UpdateAsync(user, ct);

            await _userEmailService.SendEmailConfirmationLink(sendLinkDto, ct);

            return Ok();
        }

        /// <summary>
        /// Confirms email of a user
        /// </summary>
        /// <remarks>
        /// POST: api/email/confirm
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="token">Confirmation token.</param>
        /// <returns>
        /// The result of the email confirmation.
        /// </returns>
        [HttpPost("confirm")]
        [AllowAnonymous]
        public async Task<ActionResult<ConfirmEmailResponseDto>> ConfirmEmail(
            [FromBody] ConfirmEmailRequestDto request,
            CancellationToken ct)
        {
            _logger.LogInformation($"> EmailController.ConfirmEmail");
            var result = await _userService.ConfirmEmailAsync(request.Token, ct);
            return Ok(result);
        }
    }
}