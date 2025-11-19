using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Application.Exceptions;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace Infrastructure.UseCases
{
    public class UserEmailService : IUserEmailService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IUserService _userService;
        private readonly ITokenGenerator<ConfirmationTokenDto> _confirmationTokenGenerator;
        private readonly IRazorRenderer _razorRenderer;
        private readonly IEmailSender _emailSender;
        private readonly ApiOptions _apiOptions;
        private readonly ILogger<UserService> _logger;

        public UserEmailService(
            IDbContextFactory<DataContext> dbContextFactory,
            IUserService userService,
            ITokenGenerator<ConfirmationTokenDto> confirmationTokenGenerator,
            IRazorRenderer razorRenderer,
            IEmailSender emailSender,
            IOptions<ApiOptions> apiOptions,
            ILogger<UserService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(confirmationTokenGenerator, nameof(confirmationTokenGenerator));
            ArgumentNullException.ThrowIfNull(razorRenderer, nameof(razorRenderer));
            ArgumentNullException.ThrowIfNull(emailSender, nameof(emailSender));
            ArgumentNullException.ThrowIfNull(apiOptions, nameof(apiOptions));
            ArgumentNullException.ThrowIfNull(apiOptions.Value, nameof(apiOptions.Value));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _userService = userService;
            _confirmationTokenGenerator = confirmationTokenGenerator;
            _razorRenderer = razorRenderer;
            _emailSender = emailSender;
            _apiOptions = apiOptions.Value;
            _logger = logger;
        }

        public async Task<SendEmailConfirmationResponseDto> ReSendEmailConfirmation(string email, CancellationToken ct)
        {
            var user = await _userService.GetByEmailAsync(email, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var send = await SendEmailConfirmation(user, ct);
            await _userService.UpdateAsync(user, ct);

            return send;
        }

        public async Task<SendEmailConfirmationResponseDto> SendEmailConfirmation(User user, CancellationToken ct)
        {
            if (user.EmailConfirmed)
                return new SendEmailConfirmationResponseDto("Email already confirmed.", true);

            if (!user.Active)
                throw new AccountNotActiveException("Account is currently inactive. Please contact support.");

            if (user.SecureCodeAttempts >= _apiOptions.ReSendConfirmationAttemptsMax)
            {
                user.Active = false;
                await _userService.UpdateAsync(user, ct);
                throw new ConfirmationLinkRateLimitException("User deactivated due to too many attempts.");
            }
            if (user.SecureCode != null && user.SecureCodeCreatedAt != null)
            {
                var delta = DateTime.UtcNow - user.SecureCodeCreatedAt.Value;
                if (delta.TotalSeconds < _apiOptions.ReSendConfirmationTimeoutSeconds)
                {
                    _logger.LogInformation($"UserEmailService.SendEmailConfirmation: delta={delta.TotalSeconds}sec, attempts={user.SecureCodeAttempts}");
                    var time = TimeSpan.FromSeconds(_apiOptions.ReSendConfirmationTimeoutSeconds) - delta;
                    if (time > TimeSpan.Zero)
                    {
                        var attemptsLeft = _apiOptions.ReSendConfirmationAttemptsMax - user.SecureCodeAttempts;
                        throw new ConfirmationLinkRateLimitException($"Try again in {FormatTimeSpan(time)}. Attempts left: {attemptsLeft}");
                    }
                }
            }

            ArgumentNullException.ThrowIfNullOrEmpty(user.Email, nameof(user.Email));
            _logger.LogInformation($"UserEmailService.SendEmailConfirmation Email = {user.Email}");

            var confirmationLink = GenerateEmailConfirmationLink(user, ct);
            ArgumentNullException.ThrowIfNullOrEmpty(confirmationLink, nameof(confirmationLink));

            var confirmEmailLetterDto = new ConfirmEmailLetterDto(user.UserName, confirmationLink);
            var confirmEmailHtml = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.ConfirmEmail, confirmEmailLetterDto);
            await _emailSender.SendEmailAsync(user.Email, "FlashCards: Confirm your email, please", confirmEmailHtml);

            return new SendEmailConfirmationResponseDto("Confirmation link has been sent.");
        }

        private string GenerateEmailConfirmationLink(User user, CancellationToken ct)
        {
            var confirmationToken = _confirmationTokenGenerator.GenerateToken(user);
            ArgumentNullException.ThrowIfNullOrEmpty(confirmationToken.Token, nameof(confirmationToken.Token));

            user.SecureCode = confirmationToken.Token;
            user.SecureCodeCreatedAt = DateTime.UtcNow;
            user.SecureCodeAttempts += 1;

            return String.Format(_apiOptions.ConfirmEmailUrlTemplate, confirmationToken.Token);
        }

        public async Task<ConfirmEmailResponseDto> ConfirmEmailAsync(string token, CancellationToken ct)
        {
            Guid userId = _confirmationTokenGenerator.GetUserId(token);
            return await ConfirmEmailAsync(userId, token, ct);
        }

        private async Task<ConfirmEmailResponseDto> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FindAsync(userId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (user.EmailConfirmed)
                return new ConfirmEmailResponseDto("Email already confirmed.");
            else if (user.SecureCode != token)
                throw new ConfirmationLinkMismatchException("Confirmation link is invalid or has expired.");
            else
            {
                if (_confirmationTokenGenerator.IsTokenExpired(user.SecureCode))
                    throw new ConfirmationLinkMismatchException("The link is no longer valid.");
            }

            user.EmailConfirmed = true;
            user.SecureCode = null;
            user.SecureCodeCreatedAt = null;
            var saved = await context.SaveChangesAsync(ct) > 0;

            if (saved)
                return new ConfirmEmailResponseDto("Thank you! Your email has been successfully confirmed.");
            else
                throw new ConfirmationFailedException("Failed to confirm email. Please try again or contact support.");
        }

        private string FormatTimeSpan(TimeSpan t)
        {
            if (t <= TimeSpan.Zero) return "0 sec";

            var minutes = (int)t.TotalMinutes;

            if (minutes == 0) return $"{t.Seconds} sec";

            return $"{minutes} min {t.Seconds:D2} sec";
        }
    }
}