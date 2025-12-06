using Application.Abstractions.Caching;
using Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Caching
{
    public class RedisUserCacheService : IUserCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisUserCacheService> _logger;
        private readonly TimeSpan _userTtl = TimeSpan.FromMinutes(60);

        public RedisUserCacheService(
            IDistributedCache cache, 
            ILogger<RedisUserCacheService> logger,
            IOptions<CacheServiceOptions> options)
        {
            ArgumentNullException.ThrowIfNull(cache, nameof(cache));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(options.Value, nameof(options.Value));

            _cache = cache;
            _logger = logger;
            _userTtl = TimeSpan.FromMinutes(options.Value.UserTtlMinutes);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var cacheKey = CacheKeys.User(id);
            return await GetFromCacheAsync<User>(cacheKey, ct);
        }

        //public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
        //{
        //    var cacheKey = $"username:{username.ToLowerInvariant()}";
        //    return await GetFromCacheAsync<User>(cacheKey, ct);
        //}

        //public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        //{
        //    var cacheKey = $"email:{email.ToLowerInvariant()}";
        //    return await GetFromCacheAsync<User>(cacheKey, ct);
        //}

        public async Task SetAsync(User user, CancellationToken ct)
        {
            var cacheKey = CacheKeys.User(user.Id);
            var json = JsonSerializer.Serialize(user);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _userTtl };
            await _cache.SetStringAsync(cacheKey, json, options, ct);
            _logger.LogDebug("User {UserId} cached for {Ttl}h", user.Id, _userTtl.TotalHours);
        }

        public async Task RemoveByIdAsync(Guid id, CancellationToken ct)
        {
            var cacheKey = CacheKeys.User(id);
            await _cache.RemoveAsync(cacheKey, ct);
            _logger.LogDebug("Cache removed for user {UserId}", id);
        }

        private async Task<T?> GetFromCacheAsync<T>(string cacheKey, CancellationToken ct)
        {
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<T>(cached);
            }
            _logger.LogDebug("Cache MISS: {CacheKey}", cacheKey);
            return default;
        }
    }
}