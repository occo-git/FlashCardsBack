using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Exceptions;
using Application.Extensions;
using Application.Mapping;
using Domain.Entities;
using Domain.Entities.Auth;
using FluentValidation;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Auth
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly ITokenGenerator<string> _accessTokenGenerator;
        private readonly ITokenGenerator<RefreshToken> _refreshTokenGenerator;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IDbContextFactory<DataContext> dbContextFactory,
            IRefreshTokenRepository refreshTokenRepository,
            IValidator<LoginRequestDto> loginValidator,
            ITokenGenerator<string> accessTokenGenerator,
            ITokenGenerator<RefreshToken> refreshTokenGenerator,
            ILogger<AuthenticationService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(refreshTokenRepository, nameof(refreshTokenRepository));
            ArgumentNullException.ThrowIfNull(loginValidator, nameof(loginValidator));
            ArgumentNullException.ThrowIfNull(accessTokenGenerator, nameof(accessTokenGenerator));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _refreshTokenRepository = refreshTokenRepository;
            _loginValidator = loginValidator;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _logger = logger;
        }

        public async Task<TokenResponseDto> AuthenticateAsync(LoginRequestDto loginUserDto, string sessionId, CancellationToken ct)
        {
            await _loginValidator.ValidationCheck(loginUserDto);
            _logger.LogInformation("Authenticate: Username = {Username}", loginUserDto.Username);

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == loginUserDto.Username || u.Email == loginUserDto.Username, ct);

            if (user == null || !UserMapper.CheckPassword(user, loginUserDto))
                throw new UnauthorizedAccessException("Incorrect username or password.");
            if (!user.EmailConfirmed)
                throw new EmailNotConfirmedException("Account is not confirmed.");
            if (!user.Active)
                throw new AccountNotActiveException("Account is currently inactive. Please contact support.");

            var tokens = await GenerateTokens(user, sessionId, ct);
            
            user.LastActive = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return tokens;
        }

        public async Task<TokenResponseDto> UpdateTokensAsync(string refreshToken, string sessionId, CancellationToken ct)
        {
            var oldRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(refreshToken, ct);
            if (oldRefreshToken == null || oldRefreshToken.ExpiresAt < DateTime.UtcNow || oldRefreshToken.Revoked)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FindAsync(oldRefreshToken.UserId, ct);

            if (user == null)
                throw new KeyNotFoundException("User not found.");
            if (!user.Active)
                throw new AccountNotActiveException("Account is currently inactive. Please contact support.");

            return await UpdateTokens(user, oldRefreshToken, sessionId, ct);
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            return await _refreshTokenRepository.RevokeRefreshTokensAsync(userId, sessionId, ct);
        }

        #region Tokens
        private async Task<TokenResponseDto> GenerateTokens(User user, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);

            var newAccessToken = _accessTokenGenerator.GenerateToken(user);
            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, sessionId);

            await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                newRefreshToken.SessionId);
        }
        private async Task<TokenResponseDto> UpdateTokens(User user, RefreshToken oldRefreshToken, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation("Refreshing tokens for user: {UserId}", user.Id);

            var newAccessToken = _accessTokenGenerator.GenerateToken(user);
            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, sessionId);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(oldRefreshToken, newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                newRefreshToken.SessionId);
        }
        #endregion
    }
}