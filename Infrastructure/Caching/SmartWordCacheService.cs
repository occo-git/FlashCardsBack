using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Domain.Constants;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Infrastructure.Caching
{
    public class SmartWordCacheService : ISmartWordCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger<SmartWordCacheService> _logger;

        private readonly SemaphoreSlim _preloadSemaphore = new(Levels.Length, Levels.Length);
        private readonly TimeSpan _wordsTtl = TimeSpan.FromMinutes(360);
        private readonly TimeSpan _wordsSlideTime = TimeSpan.FromMinutes(30);

        public SmartWordCacheService(
            IDistributedCache cache,
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<SmartWordCacheService> logger,
            IOptions<CacheServiceOptions> options)
        {
            ArgumentNullException.ThrowIfNull(cache, nameof(cache));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));
            ArgumentNullException.ThrowIfNull(options, nameof(options));
            ArgumentNullException.ThrowIfNull(options.Value, nameof(options.Value));

            _cache = cache;
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _wordsTtl = TimeSpan.FromMinutes(options.Value.WordsTtlMinutes);
            _wordsSlideTime = TimeSpan.FromMinutes(options.Value.WordsSlideTimeMinutes);
        }

        public async Task<List<CardDto>> GetWordsByLevelAsync(string level, CancellationToken ct)
        {
            var cacheKey = CacheKeys.WordsByLevel(level);
            var cached = await _cache.GetStringAsync(cacheKey, ct);

            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: words:level:{Level}", level);
                return JsonSerializer.Deserialize<List<CardDto>>(cached)!;
            }

            _logger.LogDebug("Cache MISS: words:level:{Level} → preload", level);

            // background loading
            _ = Task.Run(() => PreloadLevelAsync(level, ct), ct);
            return new List<CardDto>();
        }

        public async Task PreloadAllLevelsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting parallel preload of {Count} levels", Levels.Length);

            var tasks = Levels.All.Select(level => PreloadLevelAsync(level, ct)).ToArray();
            await Task.WhenAll(tasks);

            _logger.LogInformation(" • All {Count} levels preloaded successfully", Levels.Length);
        }

        private async Task PreloadLevelAsync(string level, CancellationToken ct)
        {
            await _preloadSemaphore.WaitAsync(ct);
            try
            {
                var cacheKey = CacheKeys.WordsByLevel(level);

                // the other thread could add the key already
                if (await _cache.GetStringAsync(cacheKey, ct) != null)
                {
                    _logger.LogDebug("Already cached: {Level}", level);
                    return;
                }

                _logger.LogInformation("Preloading level {Level}...", level);

                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var words = await context.Words
                    .Where(w => w.Level == level)
                    .AsNoTracking()
                    .OrderBy(w => w.Id)
                    .Select(w => w.ToCardDto(false))
                    .ToListAsync(ct);

                var json = JsonSerializer.Serialize(words, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _wordsTtl,
                    SlidingExpiration = _wordsSlideTime // slides cache expiration if it hits
                };

                await _cache.SetStringAsync(cacheKey, json, options, ct);

                _logger.LogInformation(" • Level {Level}: Loaded words - {Count}",  words.Count, level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload level {Level}", level);
            }
            finally
            {
                _preloadSemaphore.Release();
            }
        }

        public async Task<List<CardDto>> GetAllWordsAsync(CancellationToken ct)
        {
            var cacheKey = CacheKeys.WordsAll();
            var cached = await _cache.GetStringAsync(cacheKey, ct);

            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: words:all");
                return JsonSerializer.Deserialize<List<CardDto>>(cached)!;
            }

            // Load all levels in a parallel way
            var allWords = new ConcurrentBag<CardDto>();
            var tasks = Levels.All.Select(async level =>
            {
                var words = await GetWordsByLevelAsync(level, ct);
                foreach (var word in words)
                    allWords.Add(word);
            });

            await Task.WhenAll(tasks);

            var result = allWords.OrderBy(w => w.Id).ToList();
            var json = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, json,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl }, ct);

            return result;
        }

        public async Task InvalidateLevelAsync(string level, CancellationToken ct)
        {
            var cacheKey = CacheKeys.WordsByLevel(level);
            await _cache.RemoveAsync(cacheKey, ct);
            _logger.LogInformation("Invalidated cache: {Key}", cacheKey);
        }
    }
}