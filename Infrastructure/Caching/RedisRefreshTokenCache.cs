using Application.Abstractions.Caching;
using Domain.Entities.Auth;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Caching
{
    public class RedisRefreshTokenCache : IRefreshTokenCache
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisRefreshTokenCache> _logger;
        private readonly TimeSpan _refreshTokenTtl = TimeSpan.FromMinutes(20);

        public RedisRefreshTokenCache(IDistributedCache cache, ILogger<RedisRefreshTokenCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<RefreshToken?> GetAsync(string token, CancellationToken ct)
        {
            var cacheKey = $"rt:{token}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("RefreshToken cache HIT: {Token}", token[..8]);
                return JsonSerializer.Deserialize<RefreshToken>(cached);
            }
            _logger.LogDebug("RefreshToken cache MISS: {Token}", token[..8]);
            return null;
        }

        public async Task SetAsync(RefreshToken token, CancellationToken ct)
        {
            var cacheKey = $"rt:{token.Token}";
            var json = JsonSerializer.Serialize(token);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _refreshTokenTtl };
            await _cache.SetStringAsync(cacheKey, json, options, ct);
        }

        public async Task RemoveAsync(string token, CancellationToken ct)
        {
            await _cache.RemoveAsync($"rt:{token}", ct);
        }
    }
}
