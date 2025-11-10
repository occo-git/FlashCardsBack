using Domain.Entities.Auth;
using Infrastructure.DataContexts;
using Infrastructure.Repositories.Contracts;
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
        private readonly ILogger<RefreshTokenRepository> _logger;

        public RefreshTokenRepository(
            IDbContextFactory<DataContext> dbContextFactory, 
            ILogger<RefreshTokenRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
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
            //_logger.LogInformation("Getting refresh token for value {TokenValue}", tokenValue);
            //_logger.LogInformation("Getting refresh token by value");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                return await context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == tokenValue, ct);
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
                return await context.RefreshTokens
                    .Where(t => t.UserId == userId && t.SessionId == sessionId && !t.Revoked)
                    .ExecuteUpdateAsync(t => t.SetProperty(r => r.Revoked, true), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error revoking refresh tokens: UserId = {userId}, SessionId = {sessionId}");
                throw;
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string sessionId, CancellationToken ct)
        {
            _logger.LogInformation($"Validating refresh token: UserId = {userId}, SessionId = {sessionId}");
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                return await context.RefreshTokens
                    .AsNoTracking()
                    .AnyAsync(t => t.UserId == userId && t.SessionId == sessionId && t.ExpiresAt > DateTime.UtcNow && !t.Revoked, ct);
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
