using Domain.Entities.Auth;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace GatewayApi.Services.Background
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(
            IDbContextFactory<DataContext> dbContextFactory, 
            IOptions<ApiTokenOptions> options,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            if (options?.Value?.RefreshTokenCleanupIntervalMinutes > 0)
                _cleanupInterval = TimeSpan.FromMinutes(options.Value.RefreshTokenCleanupIntervalMinutes);

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CleanUpTokensAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during token cleanup");
                }

                await Task.Delay(_cleanupInterval, ct);
            }
        }

        private async Task CleanUpTokensAsync(CancellationToken ct)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            var now = DateTime.UtcNow;

            var deletedCount = await dbContext.Set<RefreshToken>()
                .Where(t => t.ExpiresAt < now || t.Revoked)
                .ExecuteDeleteAsync(ct);

            if (deletedCount > 0)
                _logger.LogInformation("Cleaned up {Count} expired or revoked tokens", deletedCount);
            else
                _logger.LogInformation("No expired or revoked tokens found for cleanup");
        }
    }
}
