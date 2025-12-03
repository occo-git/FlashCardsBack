using Application.Abstractions.Caching;
using Domain.Entities;
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
    public class UserCacheService : IUserCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<UserCacheService> _logger;
        private readonly TimeSpan userTTL = TimeSpan.FromHours(1);

        public UserCacheService(IDistributedCache cache, ILogger<UserCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct)
        {
            var cacheKey = $"user:{id}";
            return await GetFromCacheAsync<User>(cacheKey, ct);
        }

        //public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct)
        //{
        //    var cacheKey = $"username:{username.ToLowerInvariant()}";
        //    return await GetFromCacheAsync<User>(cacheKey, ct);
        //}

        //public async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct)
        //{
        //    var cacheKey = $"email:{email.ToLowerInvariant()}";
        //    return await GetFromCacheAsync<User>(cacheKey, ct);
        //}

        public async Task SetUserAsync(User user, CancellationToken ct)
        {
            var cacheKey = $"user:{user.Id}";
            var json = JsonSerializer.Serialize(user);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = userTTL };
            await _cache.SetStringAsync(cacheKey, json, options, ct);
            _logger.LogDebug("User {UserId} cached for {Ttl}h", user.Id, userTTL.TotalHours);
        }

        public async Task RemoveUserByIdAsync(Guid id, CancellationToken ct)
        {
            await _cache.RemoveAsync($"user:{id}", ct);
            _logger.LogDebug("Cache removed for user {UserId}", id);
        }

        public async Task RemoveUserByUsernameAsync(string username, CancellationToken ct)
        {
            await _cache.RemoveAsync($"username:{username.ToLowerInvariant()}", ct);
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