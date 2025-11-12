using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Text.Json;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Caching
{
    public class RedisWordCacheService : IWordCacheService
    {
        private readonly IConnectionMultiplexer _mux;
        private readonly TimeSpan _wordsTtl = TimeSpan.FromDays(7);

        public RedisWordCacheService(IConnectionMultiplexer mux)
        {
            ArgumentNullException.ThrowIfNull(mux, nameof(mux));
            _mux = mux;
        }

        private IDatabase Db => _mux.GetDatabase();

        public async Task<CardDto?> GetWordAsync(long wordId)
        {
            var json = await Db.HashGetAsync("words:all", $"word:{wordId}");
            if (json.IsNull)
                return null;
            else
                return JsonSerializer.Deserialize<CardDto>(json!);
        }

        public async Task SetWordAsync(CardDto wordDto)
        {
            var json = JsonSerializer.Serialize(wordDto);
            await Db.HashSetAsync("words:all", $"word:{wordDto.Id}", json);
            await Db.KeyExpireAsync("words:all", _wordsTtl);
        }

        //public async Task WarmupAllWordsAsync()
        //{
        //    await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        //    var words = await dbContext.Words.AsNoTracking().Select(w => w.ToCardDto()).ToListAsync();
        //    var hashEntries = words.Select(w => new HashEntry($"word:{w.Id}", JsonSerializer.Serialize(w))).ToArray();
        //    await Db.HashSetAsync("words:all", hashEntries);
        //    await Db.KeyExpireAsync("words:all", _wordsTtl);
        //}

        public async Task InvalidateAllAsync()
        {
            var server = _mux.GetServer(_mux.GetEndPoints().First());
            var keys = server.Keys(pattern: "words:*").Concat(server.Keys(pattern: "set:*")).ToArray();
            if (keys.Any())
            {
                await Db.KeyDeleteAsync(keys);
            }
        }
    }
}