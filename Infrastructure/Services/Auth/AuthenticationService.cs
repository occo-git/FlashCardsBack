using Application.Abstractions.Caching;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Exceptions;
using Application.Extensions;
using Application.Mapping;
using Application.Security;
using Application.UseCases;
using Application.Validators.Entity;
using Domain.Entities;
using Domain.Entities.Auth;
using FluentValidation;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Auth
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IUserPasswordHasher _passwordHasher;
        private readonly IUserService _userService;
        private readonly IUserEmailService _userEmailService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IValidator<TokenRequestDto> _loginValidator;
        private readonly ITokenGenerator<string> _accessTokenGenerator;
        private readonly ITokenGenerator<RefreshToken> _refreshTokenGenerator;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IDbContextFactory<DataContext> dbContextFactory,
            IUserPasswordHasher passwordHasher,
            IUserService userService,
            IUserEmailService userEmailService,
            IRefreshTokenRepository refreshTokenRepository,
            IRefreshTokenCacheService refreshTokenCache,
            IValidator<TokenRequestDto> loginValidator,
            ITokenGenerator<string> accessTokenGenerator,
            ITokenGenerator<RefreshToken> refreshTokenGenerator,
            ILogger<AuthenticationService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(passwordHasher, nameof(passwordHasher));
            ArgumentNullException.ThrowIfNull(userService, nameof(userService));
            ArgumentNullException.ThrowIfNull(userEmailService, nameof(userEmailService));
            ArgumentNullException.ThrowIfNull(refreshTokenRepository, nameof(refreshTokenRepository));
            ArgumentNullException.ThrowIfNull(loginValidator, nameof(loginValidator));
            ArgumentNullException.ThrowIfNull(accessTokenGenerator, nameof(accessTokenGenerator));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _passwordHasher = passwordHasher;
            _userService = userService;
            _userEmailService = userEmailService;
            _refreshTokenRepository = refreshTokenRepository;
            _loginValidator = loginValidator;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _logger = logger;
        }

        public async Task<User> RegisterAsync(RegisterRequestDto registerRequestDto, CancellationToken ct)
        {
            var passwordHash = _passwordHasher.HashPassword(registerRequestDto.Password);
            User newUser = UserMapper.ToDomain(registerRequestDto, passwordHash);

            var createdUser = await _userService.CreateNewAsync(newUser, ct);
            await _userEmailService.SendEmailConfirmation(createdUser, ct);
            await _userService.AddAsync(createdUser, ct);

            return newUser;
        }

        public async Task<TokenResponseDto> AuthenticateAsync(TokenRequestDto loginUserDto, string sessionId, CancellationToken ct)
        {
            await _loginValidator.ValidationCheck(loginUserDto);
            _logger.LogInformation("Authenticate: Username = {Username}", loginUserDto.Username);

            var user = await _userService.GetByUsernameOrEmailAsync(loginUserDto.Username, ct);

            if (user == null || !_passwordHasher.VerifyHashedPassword(user.PasswordHash, loginUserDto.Password!))
                throw new UnauthorizedAccessException("Incorrect username or password.");
            UserValidator.ValidateActiveUser(user);

            var tokens = await GenerateTokens(user, loginUserDto.ClientId, sessionId, ct);

            await _userService.UpdateAsync(user, ct);

            return tokens;
        }

        public async Task<TokenResponseDto> AuthenticateGoogleUserAsync(string email, string clientId, string sessionId, CancellationToken ct)
        {
            bool isNewUser = false;
            var user = await _userService.GetByEmailAsync(email, ct);
            if (user == null)
            {
                user = await _userService.CreateNewGoogleUserAsync(email, ct);
                isNewUser = true;
            }

            ArgumentNullException.ThrowIfNull(user, nameof(user));
            _logger.LogInformation("AuthenticateGoogleUser: Username = {Username}", user.UserName);

            UserValidator.ValidateActiveUser(user);
            var tokens = await GenerateTokens(user, clientId, sessionId, ct);

            await _userService.UpdateAsync(user, ct);

            if (isNewUser)
                await _userEmailService.SendGreeting(user, ct);

            return tokens;
        }

        public async Task<TokenResponseDto> UpdateTokensAsync(TokenRequestDto request, string sessionId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(request.RefreshToken, nameof(request.RefreshToken));

            var oldRefreshToken = await _refreshTokenRepository.GetRefreshTokenAsync(request.RefreshToken, ct);
            if (oldRefreshToken == null || oldRefreshToken.ExpiresAt < DateTime.UtcNow || oldRefreshToken.Revoked)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FindAsync(oldRefreshToken.UserId, ct);

            if (user == null)
                throw new KeyNotFoundException("User not found.");
            if (!user.Active)
                throw new AccountNotActiveException("Account is currently inactive. Please contact support.");

            return await UpdateTokens(user, oldRefreshToken, request.ClientId, sessionId, ct);
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            return await _refreshTokenRepository.RevokeRefreshTokensAsync(userId, sessionId, ct);
        }

        #region Tokens
        private async Task<TokenResponseDto> GenerateTokens(User user, string clientId, string sessionId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            _logger.LogInformation("Generating tokens for user: {UserId}", user.Id);

            var newAccessToken = _accessTokenGenerator.GenerateToken(user, clientId);
            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, clientId, sessionId);

            await _refreshTokenRepository.AddRefreshTokenAsync(newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                _accessTokenGenerator.ExpiresInSeconds,
                newRefreshToken.SessionId);
        }
        private async Task<TokenResponseDto> UpdateTokens(User user, RefreshToken oldRefreshToken, string clientId, string sessionId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            _logger.LogInformation("Refreshing tokens for user: {UserId}", user.Id);
            
            ArgumentNullException.ThrowIfNull(oldRefreshToken, nameof(oldRefreshToken));
            var newAccessToken = _accessTokenGenerator.GenerateToken(user, clientId);
            var newRefreshToken = _refreshTokenGenerator.GenerateToken(user, clientId, sessionId);

            await _refreshTokenRepository.UpdateRefreshTokenAsync(oldRefreshToken, newRefreshToken, ct);

            return new TokenResponseDto(
                newAccessToken, 
                newRefreshToken.Token,
                _accessTokenGenerator.ExpiresInSeconds,
                newRefreshToken.SessionId);
        }
        #endregion
    }
}