using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Email;
using Application.DTO.Email.Letters;
using Application.DTO.Users.EmailConfirmation;
using Application.DTO.Users.ResetPassword;
using Application.Exceptions;
using Application.UseCases;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace Infrastructure.UseCases
{
    public class UserEmailService : IUserEmailService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IUserService _userService;
        private readonly IJwtTokenReader _tokenReader;
        private readonly IRazorRenderer _razorRenderer;
        private readonly IEmailSender _emailSender;
        private readonly ApiOptions _apiOptions;
        private readonly ApiTokenOptions _apiTokenOptions;
        private readonly ILogger<UserService> _logger;

        public UserEmailService(
            IServiceScopeFactory scopeFactory,
            IUserService userService,
            IJwtTokenReader tokenReader,
            IRazorRenderer razorRenderer,
            IEmailSender emailSender,
            IOptions<ApiOptions> apiOptions,
            IOptions<ApiTokenOptions> apiTokenOptions,
            ILogger<UserService> logger)
        {
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(scopeFactory, nameof(scopeFactory));
            ArgumentNullException.ThrowIfNull(tokenReader, nameof(tokenReader));
            ArgumentNullException.ThrowIfNull(razorRenderer, nameof(razorRenderer));
            ArgumentNullException.ThrowIfNull(emailSender, nameof(emailSender));
            ArgumentNullException.ThrowIfNull(apiOptions, nameof(apiOptions));
            ArgumentNullException.ThrowIfNull(apiOptions.Value, nameof(apiOptions.Value));
            ArgumentNullException.ThrowIfNull(apiTokenOptions, nameof(apiTokenOptions));
            ArgumentNullException.ThrowIfNull(apiTokenOptions.Value, nameof(apiTokenOptions.Value));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _userService = userService;
            _scopeFactory = scopeFactory;
            _tokenReader = tokenReader;
            _razorRenderer = razorRenderer;
            _emailSender = emailSender;
            _apiOptions = apiOptions.Value;
            _apiTokenOptions = apiTokenOptions.Value;
            _logger = logger;
        }

        #region Greeting
        public async Task SendGreeting(User user, CancellationToken ct)
        {
            if (!user.Active)
                await SendNotActiveAccount(user, "a login", ct);

            ArgumentNullException.ThrowIfNullOrEmpty(user.Email, nameof(user.Email));
            _logger.LogInformation($"UserEmailService.SendEmailConfirmation Email = {user.Email}");

            var greetingLetterDto = new GreetingLetterDto(user.UserName, user.Provider, _apiOptions.LoginUrl);

            var html = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.Greeting, greetingLetterDto);
            var emailDto = new SendEmailDto(user.Email, "FlashCards: Welcome!", html);

            await QueueEmailAsync(emailDto, ct);
            //await _emailSender.SendEmailAsync(emailDto, ct);
        }
        #endregion

        #region Information
        public async Task SendUsernameChanged(User user, string newUsername, CancellationToken ct)
        {
            if (!user.Active)
                await SendNotActiveAccount(user, "a username change", ct);

            var informationLetterDto = new InformationLetterDto(
                "Username Changed",
                "Username changed!",
                new string[]
                {
                    "Thank you for using your account!",
                    $"Your username has been successfully updated to: {newUsername}",
                    "Your account is secure and ready to use."
                },
                _apiOptions.LoginUrl);

            await SendInformation(user, informationLetterDto, ct);

        }
        public async Task SendPasswordChanged(User user, CancellationToken ct)
        {
            if (!user.Active)
                await SendNotActiveAccount(user, "a password change", ct);

            var informationLetterDto = new InformationLetterDto(
                "Password Changed",
                "Password changed!",
                new string[]
                {
                    "Thank you for using your account!",
                    "Your password has been successfully updated.",
                    "Your account is secure and ready to use."
                },
                _apiOptions.LoginUrl);

            await SendInformation(user, informationLetterDto, ct);
        }
        private async Task SendNotActiveAccount(User user, string actionName, CancellationToken ct)
        {
            var informationLetterDto = new InformationLetterDto(
                "Account Inactive",
                "Account Inactive!",
                new string[]
                {
                    $"We noticed {actionName} attempt to your account.",
                    "However, your account is currently marked as inactive.",
                    "If this was you, please contact our support team to reactivate your account."
                },
                _apiOptions.LoginUrl);
            await SendInformation(user, informationLetterDto, ct);
        }
        private async Task SendInformation(User user, InformationLetterDto informationLetterDto, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(user.Email, nameof(user.Email));
            _logger.LogInformation($"UserEmailService.SendEmailConfirmation Email = {user.Email}");

            var html = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.Information, informationLetterDto);
            var emailDto = new SendEmailDto(user.Email, "FlashCards: Information", html);

            await QueueEmailAsync(emailDto, ct);
            //await _emailSender.SendEmailAsync(emailDto, ct);
        }
        #endregion

        #region Email Confirmation
        public async Task SendEmailConfirmationLink(SendLinkDto sendLinkDto, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(sendLinkDto.ToEmail, nameof(sendLinkDto.ToEmail));
            _logger.LogInformation($"UserEmailService.SendEmailConfirmationLink Email = {sendLinkDto.ToEmail}");

            var confirmEmailLetterDto = new ConfirmEmailLetterDto(sendLinkDto.ToName, sendLinkDto.Link, _apiTokenOptions.ConfirmationTokenExpiresMinutes);
            var html = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.ConfirmEmail, confirmEmailLetterDto);
            var emailDto = new SendEmailDto(sendLinkDto.ToEmail, "FlashCards: Confirm your email, please", html);
            await QueueEmailAsync(emailDto, ct);
            //await _emailSender.SendEmailAsync(emailDto, ct);
        }
        #endregion

        #region Reset Password
        public async Task SendResetPasswordLink(SendLinkDto sendLinkDto, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(sendLinkDto.ToEmail, nameof(sendLinkDto.ToEmail));
            _logger.LogInformation($"UserEmailService.SendResetPasswordRequestLink Email = {sendLinkDto.ToEmail}");

            var resetPasswordLetterDto = new ResetPasswordLetterDto(sendLinkDto.ToName, sendLinkDto.Link, _apiTokenOptions.ResetPasswordTokenExpiresMinutes);
            var html = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.ResetPassword, resetPasswordLetterDto);
            var emailDto = new SendEmailDto(sendLinkDto.ToEmail, "FlashCards: Reset your password", html);
            await QueueEmailAsync(emailDto, ct);
            //await _emailSender.SendEmailAsync(emailDto, ct);
        }
        #endregion

        #region Helpers
        private async Task QueueEmailAsync(SendEmailDto emailDto, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var emailQueue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
            await emailQueue.QueueEmailAsync(emailDto, ct);
        }
        #endregion
    }
}