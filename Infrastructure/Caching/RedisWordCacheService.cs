using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Domain.Constants;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Shared.Configuration;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Text.Json;

namespace Infrastructure.Caching
{
    public class RedisWordCacheService : IRedisWordCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger<RedisWordCacheService> _logger;

        private readonly SemaphoreSlim _preloadSemaphore = new(Levels.Length, Levels.Length);
        private readonly TimeSpan _wordsTtl = TimeSpan.FromMinutes(360);
        private readonly TimeSpan _wordsSlideTime = TimeSpan.FromMinutes(30);
        private readonly TimeSpan _bookmarksTtl = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _bookmarksSlideTime = TimeSpan.FromMinutes(2);

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        public RedisWordCacheService(
            IDistributedCache cache,
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<RedisWordCacheService> logger,
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
            _bookmarksTtl = TimeSpan.FromMinutes(options.Value.BookmarksTtlMinutes);
            _bookmarksSlideTime = TimeSpan.FromMinutes(options.Value.BookmarksSlideTimeMinutes);
        }

        #region Preload
        public async Task PreloadAllLevelsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting parallel preload of {Count} levels", Levels.Length);

            // Words
            var wordTasks = Levels.All.Select(level => PreloadWordsByLevelAsync(level, ct)).ToArray();
            await Task.WhenAll(wordTasks);

            // Themes
            var themeTasks = Levels.All.Select(level => PreloadThemesByLevelAsync(level, ct)).ToArray();
            await Task.WhenAll(themeTasks);

            _logger.LogInformation(" • All {Count} levels preloaded successfully", Levels.Length);
        }

        private async Task PreloadWordsByLevelAsync(string level, CancellationToken ct)
        {
            await _preloadSemaphore.WaitAsync(ct);
            try
            {
                var cacheKey = CacheKeys.WordsByLevel(level);
                if (await _cache.GetStringAsync(cacheKey, ct) != null) // the other thread could add the key already
                {
                    _logger.LogInformation(" • Level {Level}: Words already cached!", level);
                    return;
                }

                _logger.LogInformation(" • Level {Level}: Preloading words ...", level);
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var words = await context.Words
                    .Where(w => w.Level == level)
                    .AsNoTracking()
                    .OrderBy(w => w.Id)
                    .Select(w => w.ToCardDto(false))
                    .ToListAsync(ct);

                var json = JsonSerializer.Serialize(words, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _wordsTtl,
                    SlidingExpiration = _wordsSlideTime // slides cache expiration if it hits
                };
                await _cache.SetStringAsync(cacheKey, json, options, ct);

                _logger.LogInformation(" • Level {Level}: Loaded words - {Count}", level, words.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " ! Level {Level}: Failed to preload words!", level);
            }
            finally
            {
                _preloadSemaphore.Release();
            }
        }

        private async Task PreloadThemesByLevelAsync(string level, CancellationToken ct)
        {
            await _preloadSemaphore.WaitAsync(ct);
            try
            {
                var cacheKey = CacheKeys.ThemesByLevel(level);
                var cached = await _cache.GetStringAsync(cacheKey);
                if (cached == null)
                {
                    _logger.LogInformation(" • Level {Level}: Preloading themes ...", level);
                    using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                    var themes = await context.Themes
                        .Where(t => t.WordThemes.Any(wt => wt.Word != null && wt.Word.Level == level))
                        .AsNoTracking()
                        .OrderBy(t => t.Name)
                        .Select(t => t.ToDto(t.WordThemes.Count(wt => wt.Word != null && wt.Word.Level == level)))
                        .ToListAsync(ct);

                    var json = JsonSerializer.Serialize(themes, _jsonOptions);
                    var options = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _wordsTtl,
                        SlidingExpiration = _wordsSlideTime
                    };
                    await _cache.SetStringAsync(cacheKey, json, options, ct);
                    _logger.LogInformation(" • Level {Level}: Loaded themes - {Count}", level, themes.Count);

                    // words by theme
                    if (themes != null)
                    {
                        var preloadTasks = themes.Select(theme => PreloadWordsByThemeAsync(theme.Id, ct));
                        await Task.WhenAll(preloadTasks);
                    }
                }
                else
                {
                    _logger.LogInformation(" • Level {Level}: Themes already cached!", level);
                    var themes = JsonSerializer.Deserialize<List<ThemeDto>>(cached);

                    // words by theme
                    if (themes != null)
                    {
                        var preloadTasks = themes.Select(theme => PreloadWordsByThemeAsync(theme.Id, ct));
                        await Task.WhenAll(preloadTasks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " ! Level {Level}: Failed to preload themes!", level);
            }
            finally
            {
                _preloadSemaphore.Release();
            }
        }

        private async Task PreloadWordsByThemeAsync(long themeId, CancellationToken ct)
        {
            await _preloadSemaphore.WaitAsync(ct);
            try
            {
                var cacheKey = CacheKeys.WordsByTheme(themeId);
                if (await _cache.GetStringAsync(cacheKey, ct) != null)
                {
                    _logger.LogInformation(" • ThemeId = {ThemeId}: Words already cached!", themeId);
                    return;
                }

                _logger.LogInformation(" • ThemeId = {ThemeId}: Preloading words ...", themeId);
                using var context = await _dbContextFactory.CreateDbContextAsync(ct);
                var words = await context.WordThemes
                    .Where(wt => wt.ThemeId == themeId)
                    .Join(context.Words,
                        wt => wt.WordId,
                        w => w.Id,
                        (wt, w) => w)
                    .AsNoTracking()
                    .OrderBy(w => w.Id)
                    .Select(w => w.ToCardDto(false))
                    .ToListAsync(ct);

                var json = JsonSerializer.Serialize(words, _jsonOptions);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _wordsTtl,
                    SlidingExpiration = _wordsSlideTime
                };
                await _cache.SetStringAsync(cacheKey, json, options, ct);
                _logger.LogInformation(" • ThemeId = {ThemeId}: Loaded words - {Count}", themeId, words.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " ! ThemeId = {ThemeId}: Failed to preload words!", themeId);
            }
            finally
            {
                _preloadSemaphore.Release();
            }
        }
        #endregion

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
            _ = Task.Run(() => PreloadWordsByLevelAsync(level, ct), ct);
            return new List<CardDto>();
        }
        public async Task<HashSet<long>> GetWordIdsByLevelAsync(string level, CancellationToken ct)
        {
            var words = await GetWordsByLevelAsync(level, ct); // List<CardDto>
            return words.Select(w => w.Id).ToHashSet();
        }

        public async Task<List<ThemeDto>> GetThemesByLevelAsync(string level, CancellationToken ct)
        {
            var cacheKey = CacheKeys.ThemesByLevel(level);
            var cached = await _cache.GetStringAsync(cacheKey, ct);

            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: themes:level:{Level}", level);
                return JsonSerializer.Deserialize<List<ThemeDto>>(cached)!;
            }

            _logger.LogDebug("Cache MISS: themes:level:{Level} → preload", level);

            // background loading
            _ = Task.Run(() => PreloadThemesByLevelAsync(level, ct), ct);
            return new List<ThemeDto>();
        }

        public async Task<List<CardDto>> GetWordsByThemeAsync(long themeId, CancellationToken ct)
        {
            var cacheKey = CacheKeys.WordsByTheme(themeId);
            var cached = await _cache.GetStringAsync(cacheKey, ct);

            if (cached != null)
            {
                _logger.LogDebug("Cache HIT: words:theme:{ThemeId}", themeId);
                return JsonSerializer.Deserialize<List<CardDto>>(cached)!;
            }

            _logger.LogDebug("Cache MISS: words:theme:{ThemeId} → preload", themeId);

            // background loading
            _ = Task.Run(() => PreloadWordsByThemeAsync(themeId, ct), ct);
            return new List<CardDto>();
        }
        public async Task<HashSet<long>> GetWordIdsByThemeAsync(long themeId, CancellationToken ct)
        {
            var words = await GetWordsByThemeAsync(themeId, ct);
            return words.Select(w => w.Id).ToHashSet();
        }

        public async Task<HashSet<long>> GetUserBookmarksAsync(Guid userId, CancellationToken ct)
        {
            var cacheKey = CacheKeys.UserBookmark(userId);
            var json = await _cache.GetStringAsync(cacheKey, ct);
            if (json != null)
                return JsonSerializer.Deserialize<List<long>>(json)!.ToHashSet();

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var bookmarkWordIds = await context.UserBookmarks
                .Where(b => b.UserId == userId)
                .AsNoTracking()
                .Select(b => b.WordId)
                .ToListAsync(ct);

            var resultJson = JsonSerializer.Serialize(bookmarkWordIds, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _bookmarksTtl,
                SlidingExpiration = _bookmarksSlideTime
            };
            await _cache.SetStringAsync(cacheKey, resultJson, options, ct);

            return bookmarkWordIds.ToHashSet();
        }

        public async Task InvalidateBookmarksAsync(Guid userId, CancellationToken ct)
        { 
            await InvalidateKeyAsync(CacheKeys.UserBookmark(userId), ct);
        }

        public async Task InvalidateByLevelAsync(string level, CancellationToken ct)
        {
            await InvalidateKeyAsync(CacheKeys.WordsByLevel(level), ct);
            await InvalidateKeyAsync(CacheKeys.ThemesByLevel(level), ct);

            //TODO: Invalidate Words by Themes
        }

        private async Task InvalidateKeyAsync(string key, CancellationToken ct)
        {
            await _cache.RemoveAsync(key, ct);
            _logger.LogInformation("Invalidated cache: {Key}", key);

        }
    }
}