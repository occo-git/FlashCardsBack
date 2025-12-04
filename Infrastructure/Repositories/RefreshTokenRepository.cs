using Application.Abstractions.Caching;
using Application.Abstractions.Repositories;
using Domain.Entities.Auth;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IRefreshTokenCacheService _refreshTokenCache;
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(
            IDbContextFactory<DataContext> dbContextFactory,
            IRefreshTokenCacheService refreshTokenCache,
            ILogger<RefreshTokenRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(refreshTokenCache, nameof(refreshTokenCache));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _refreshTokenCache = refreshTokenCache;
            _logger = logger;
        }

        public async Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct)
        {
            await RevokeRefreshTokensAsync(refreshToken.UserId, refreshToken.SessionId, ct);

            _logger.LogInformation($"Adding refresh token: UserId = {refreshToken.UserId}");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                await context.RefreshTokens.AddAsync(refreshToken, ct);
                await context.SaveChangesAsync(ct);
                return refreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding refresh token: UserId = {refreshToken.UserId}");
                throw;
            }
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenValue, CancellationToken ct)
        {
            // Cache-Aside for refresh token
            var cachedToken = await _refreshTokenCache.GetAsync(tokenValue, ct);
            if (cachedToken != null)
                return cachedToken;

            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var dbToken = await context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == tokenValue, ct);
                                
                if (dbToken != null)
                    await _refreshTokenCache.SetAsync(dbToken, ct); // Cache set
                return dbToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refresh token");
                throw;
            }
        }

        public async Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation($"Revoking refresh tokens: UserId = {userId}, SessionId = {sessionId}");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var result = await context.RefreshTokens
                    .Where(t => t.UserId == userId && t.SessionId == sessionId && !t.Revoked)
                    .ExecuteUpdateAsync(t => t.SetProperty(r => r.Revoked, true), ct);

                // does not need to invalidate refresh token cache, because TTL = 20 min
                await _refreshTokenCache.RemoveValidationAsync(userId, sessionId, ct); // Cache invalidate
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking refresh tokens: UserId = {userId}, SessionId = {sessionId}");
                throw;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            // Cache-Aside for refresh token
            var isValid = await _refreshTokenCache.GetValidationAsync(userId, sessionId, ct);
            if (isValid != null)
                return isValid.Value;

            _logger.LogInformation($"Validating refresh token: UserId = {userId}, SessionId = {sessionId}");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var result =  await context.RefreshTokens
                    .AsNoTracking()
                    .AnyAsync(t => t.UserId == userId && t.SessionId == sessionId && t.ExpiresAt > DateTime.UtcNow && !t.Revoked, ct);

                await _refreshTokenCache.SetValidationAsync(userId, sessionId, result, ct); // Cache set
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating refresh token: UserId = {userId}, SessionId = {sessionId}");
                throw;
            }
        }

        public async Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken oldRefreshToken, RefreshToken newRefreshToken, CancellationToken ct)
        {
            //_logger.LogInformation("Updating refresh token from {OldValue} to {newToken}", oldRefreshToken.Token, newRefreshToken.Token);
            _logger.LogInformation("Updating refresh token");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                if (oldRefreshToken != null)
                {
                    oldRefreshToken.Revoked = true;
                    context.RefreshTokens.Update(oldRefreshToken);
                    
                    await _refreshTokenCache.RemoveAsync(oldRefreshToken.Token, ct); // Cache invalidate
                }

                await context.RefreshTokens.AddAsync(newRefreshToken, ct);
                await context.SaveChangesAsync(ct);

                return newRefreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refresh token");
                throw;
            }
        }
    }
}