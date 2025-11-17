using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Email;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [Route("api/email")]
    public class EmailController : Controller
    {
        private readonly IUserEmailService _userEmailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IUserEmailService userEmailService,
            ILogger<EmailController> logger)
        {
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

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
        //[HttpGet("confirm/{userId:guid}/{*token}")]
        //[AllowAnonymous]
        //public async Task<ActionResult<ConfirmEmailResponseDto>> ConfirmEmail(Guid userId, string token, CancellationToken ct)
        //{
        //    _logger.LogInformation($"> EmailController.ConfirmEmail UserId = {userId}");

        //    try
        //    {
        //        var result = await _userEmailService.ConfirmEmailAsync(userId, token, ct);
        //        if (IsHtmlRequest())
        //            return View("ConfirmationResult", result);
        //        else
        //            return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (IsHtmlRequest())
        //            return View("~/Views/Error/GeneralError.cshtml", ex);
        //        else
        //            throw;
        //    }
        //}

        //private bool IsHtmlRequest() => Request.Headers["Accept"].Any(h => h!.Contains("text/html"));
    }
}
