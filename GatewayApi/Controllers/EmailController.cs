using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Email;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Shared;

namespace GatewayApi.Controllers
{
    [Route("api/email")]
    public class EmailController : Controller
    {
        private readonly IUserEmailService _userEmailService;
        private readonly IUserService _userService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IUserEmailService userEmailService,
            IUserService userService,
            ILogger<EmailController> logger)
        {
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _userEmailService = userEmailService;
            _userService = userService;
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
        public async Task<SendEmailConfirmationResponseDto> ReSendEmailConfirmation(
            [FromBody] SendEmailConfirmationRequestDto request,
            CancellationToken ct)
        {
            _logger.LogInformation($"> EmailController.ReSendEmailConfirmation");
            return await _userEmailService.ReSendEmailConfirmation(request.Email, ct);
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
        public async Task<ConfirmEmailResponseDto> ConfirmEmail(
            [FromBody] ConfirmEmailRequestDto request,
            CancellationToken ct)
        {
            _logger.LogInformation($"> EmailController.ConfirmEmail");
            return await _userEmailService.ConfirmEmailAsync(request.Token, ct);
        }
    }
}