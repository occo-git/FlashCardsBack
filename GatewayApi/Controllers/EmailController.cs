using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Email;
using Application.UseCases;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace GatewayApi.Controllers
{
    [Route("api/email")]
    public class EmailController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRazorRenderer _razorRenderer;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IUserService userService,
            IRazorRenderer razorRenderer,
            IEmailSender emailSender,
            ILogger<EmailController> logger)
        {
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(razorRenderer, nameof(razorRenderer));
            ArgumentNullException.ThrowIfNull(emailSender, nameof(emailSender));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _userService = userService;
            _razorRenderer = razorRenderer;
            _emailSender = emailSender;
            _logger = logger;
        }

        /// <summary>
        /// Confirms email of a user
        /// </summary>
        /// <remarks>
        /// GET: api/email/confirm
        /// This endpoint is open to anonymous users.
        /// </remarks>
        /// <param name="userId">Id of the user.</param>
        /// <param name="token">Confirmation token.</param>
        /// <returns>
        /// The result of the email confirmation.
        /// </returns>
        [HttpGet("confirm/{userId:guid}/{*token}")]
        [AllowAnonymous]
        public async Task<ActionResult<ConfirmEmailResponseDto>> ConfirmEmail(Guid userId, string token, CancellationToken ct)
        {
            _logger.LogInformation($"> UsersController.ConfirmEmail UserId = {userId}");

            try
            {
                var result = await _userService.ConfirmEmailAsync(userId, token, ct);
                if (IsHtmlRequest())
                    return View("ConfirmationResult", result);
                else
                    return result;
            }
            catch (Exception ex)
            {
                if (IsHtmlRequest())
                    return View("~/Views/Error/GeneralError.cshtml", ex);
                else
                    throw;
            }
        }

        private bool IsHtmlRequest() => Request.Headers["Accept"].Any(h => h!.Contains("text/html"));
    }
}
