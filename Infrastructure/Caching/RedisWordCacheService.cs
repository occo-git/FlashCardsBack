// Infrastructure/Caching/RedisWordCacheService.cs
using Domain.Entities.Words;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Text.Json;

namespace Infrastructure.Caching;

//public class RedisWordCacheService : IWordCacheService
//{
//    private readonly IDistributedCache _distributedCache;
//    private readonly IConnectionMultiplexer _mux;
//    private readonly TimeSpan _wordTtl = TimeSpan.FromDays(7);
//    private readonly TimeSpan _pageTtl = TimeSpan.FromDays(30);

//    private IDatabase Db => _mux.GetDatabase();

//    public RedisWordCacheService(
//        IDistributedCache distributedCache,
//        IConnectionMultiplexer mux)
//    {
//        _distributedCache = distributedCache;
//        _mux = mux;
//    }

//    public async Task<CardDto?> GetCardAsync(long wordId)
//    {
//        var json = await Db.HashGetAsync("words:all", $"word:{wordId}");
//        return json.IsNull ? null : JsonSerializer.Deserialize<CardDto>(json!);
//    }

//    public async Task<CardDto[]?> GetPageAsync(long setId, int page)
//    {
//        var key = $"set:{setId}:page:{page}";
//        var json = await _distributedCache.GetStringAsync(key);
//        return json is null ? null : JsonSerializer.Deserialize<CardDto[]>(json);
//    }

//    public async Task WarmupWordAsync(Word word)
//    {
//        var dto = word.ToCardDto(); // ваш маппер
//        var json = JsonSerializer.Serialize(dto);

//        await Db.HashSetAsync("words:all", $"word:{word.Id}", json);
//        await Db.KeyExpireAsync("words:all", _wordTtl);
//    }

//    public async Task WarmupSetPageAsync(long setId, int page)
//    {
//        // Предполагаем, что у тебя есть способ получить слова для страницы
//        var words = await GetWordsForPageFromDb(setId, page); // ваш метод
//        var dtos = words.Select(w => w.ToCardDto()).ToArray();
//        var json = JsonSerializer.Serialize(dtos);

//        var key = $"set:{setId}:page:{page}";
//        await _distributedCache.SetStringAsync(key, json, new DistributedCacheEntryOptions
//        {
//            AbsoluteExpirationRelativeToNow = _pageTtl
//        });
//    }

//    public async Task InvalidateWordAsync(long wordId)
//    {
//        await Db.HashDeleteAsync("words:all", $"word:{wordId}");
//        // Если нужно — можно дополнительно удалить страницы, где это слово
//        // (можно хранить обратный индекс setId → pages)
//    }

//    public async Task InvalidateSetAsync(long setId)
//    {
//        var server = _mux.GetServer(_mux.GetEndPoints().First());
//        var keys = server.Keys(pattern: $"set:{setId}:page:*").ToArray();
//        if (keys.Length > 0)
//            await Db.KeyDeleteAsync(keys);
//    }

//    // Вспомогательный метод — замените на свой репозиторий
//    private async Task<Word[]> GetWordsForPageFromDb(long setId, int page, int pageSize = 20)
//    {
//        // Пример с EF Core
//        using var scope = _distributedCache.CreateScope(); // если нужен DbContext
//        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//        return await db.WordSetItems
//            .Where(wsi => wsi.WordSetId == setId)
//            .OrderBy(wsi => wsi.Order)
//            .Skip(page * pageSize)
//            .Take(pageSize)
//            .Include(wsi => wsi.Word)
//            .Select(wsi => wsi.Word)
//            .ToArrayAsync();
//    }
//}