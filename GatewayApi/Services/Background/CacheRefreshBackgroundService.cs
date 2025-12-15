using Application.Abstractions.Caching;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace GatewayApi.Services.Background
{
    public class CacheRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(240);

        public CacheRefreshBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CacheRefreshBackgroundService> logger,
            IOptions<CacheServiceOptions> options)
        {
            ArgumentNullException.ThrowIfNull(scopeFactory, nameof(scopeFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(options.Value, nameof(options.Value));

            _scopeFactory = scopeFactory;
            _logger = logger;
            _refreshInterval = TimeSpan.FromMinutes(options.Value.CacheRefreshIntervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_refreshInterval, ct);
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<IRedisWordCacheService>();
                    await cache.PreloadAllLevelsAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cache refreshing");
                }
            }
        }
    }
}