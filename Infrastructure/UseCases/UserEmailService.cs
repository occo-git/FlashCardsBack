using Application.Abstractions.DataContexts;
using Application.Abstractions.Services;
using Application.DTO;
using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Users;
using Infrastructure.DataContexts;
using Infrastructure.Services.EmailSender;
using Infrastructure.Services.RazorRenderer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<SendEmailConfirmationResponseDto> SendEmailConfirmation(string token, CancellationToken ct)
        {
            Guid userId = _confirmationTokenGenerator.GetUserId(token);
            var user = await _userService.GetByIdAsync(userId, ct);
            return await SendEmailConfirmation(user, ct);
        }

        public async Task<SendEmailConfirmationResponseDto> SendEmailConfirmation(User? user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (user.EmailConfirmed)
                return new SendEmailConfirmationResponseDto(false, "Email already confirmed.");

            ArgumentNullException.ThrowIfNullOrEmpty(user.Email, nameof(user.Email));
            _logger.LogInformation($"UserEmailService.SendEmailConfirmation Email = {user.Email}");

            var confirmationLink = await GenerateEmailConfirmationLinkAsync(user.Id, ct);
            ArgumentNullException.ThrowIfNullOrEmpty(confirmationLink, nameof(confirmationLink));

            var confirmEmailLetterDto = new ConfirmEmailLetterDto(user.UserName, confirmationLink);
            var confirmEmailHtml = await _razorRenderer.RenderViewToStringAsync(RenderTemplates.ConfirmEmail, confirmEmailLetterDto);
            await _emailSender.SendEmailAsync(user.Email, "[FlashCards] - Confirm your email, please", confirmEmailHtml);

            return new SendEmailConfirmationResponseDto(true, "Confirmation link has been sent.");
        }

        private async Task<string> GenerateEmailConfirmationLinkAsync(Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            var confirmationToken = _confirmationTokenGenerator.GenerateToken(existingUser);
            ArgumentNullException.ThrowIfNullOrEmpty(confirmationToken.Token, nameof(confirmationToken.Token));

            existingUser.SecureCode = confirmationToken.Token;
            await context.SaveChangesAsync(ct);

            return String.Format(_apiOptions.ConfirmEmailUrlTemplate, confirmationToken.Token);
        }

        public async Task<ConfirmEmailResponseDto> ConfirmEmailAsync(string token, CancellationToken ct)
        {
            Guid userId = _confirmationTokenGenerator.GetUserId(token);
            return await ConfirmEmailAsync(userId, token, ct);
        }

        public async Task<ConfirmEmailResponseDto> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            if (existingUser.EmailConfirmed)
                return new ConfirmEmailResponseDto(true, "Email already confirmed.", _apiOptions.LoginUrl);
            else if (existingUser.SecureCode != token)
                return new ConfirmEmailResponseDto(false, "Failed to confirm email. Confirmation token not found. Please try again or contact support.");

            //TODO: Check token expiration!

            existingUser.EmailConfirmed = true;
            var saved = await context.SaveChangesAsync(ct) > 0;

            if (saved)
                return new ConfirmEmailResponseDto(true, "Thank you! Your email has been successfully confirmed.", _apiOptions.LoginUrl);
            else
                return new ConfirmEmailResponseDto(false, "Failed to confirm email. Please try again or contact support.");
        }
    }
}