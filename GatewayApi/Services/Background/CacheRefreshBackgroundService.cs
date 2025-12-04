using Application.Abstractions.Caching;
using Microsoft.EntityFrameworkCore.Internal;

namespace GatewayApi.Services.Background
{
    public class CacheRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(4);

        public CacheRefreshBackgroundService(IServiceProvider services, ILogger<CacheRefreshBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_refreshInterval, ct);
                try
                {
                    using var scope = _services.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<ISmartWordCacheService>();
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