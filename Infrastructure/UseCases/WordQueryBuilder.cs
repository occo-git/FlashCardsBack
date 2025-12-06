using Application.Abstractions.Caching;
using Application.Abstractions.DataContexts;
using Application.DTO.Words;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities.Words;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases
{
    public class WordQueryBuilder : IWordQueryBuilder
    {
        private readonly IRedisWordCacheService _cache;
        public WordQueryBuilder(IRedisWordCacheService cache)
        {
            ArgumentNullException.ThrowIfNull(cache, nameof(cache));
            _cache = cache;
        }

        public IQueryable<Word> BuildDbQuery(IDataContext dbContext, DeckFilterDto filter, Guid userId)
        {
            var query = filter.ThemeId > 0 ?
                dbContext.Words.AsNoTracking().Where(w => w.WordThemes.Any(t => t.ThemeId == filter.ThemeId)) :
                dbContext.Words.AsNoTracking().Where(w => w.Level == filter.Level);

            if (filter.Difficulty > 0)
                query = query.Where(w => w.Difficulty == filter.Difficulty);

            if (filter.IsMarked != 0)
            {
                if (filter.IsMarked == 1)
                    query = query.Where(w => w.Bookmarks.Any(b => b.UserId == userId));
                else if (filter.IsMarked == -1)
                    query = query.Where(w => !w.Bookmarks.Any(b => b.UserId == userId));
            }
            return query;
        }
        public async Task<IQueryable<Word>> BuildQueryCachedAsync(IDataContext dbContext, DeckFilterDto filter, Guid userId, CancellationToken ct)
        {
            IQueryable<Word> query;

            if (filter.ThemeId > 0)
            {
                var themeWordIds = await _cache.GetWordIdsByThemeAsync(filter.ThemeId, ct);
                query = dbContext.Words
                    .AsNoTracking()
                    .Where(w => themeWordIds.Contains(w.Id));
            }
            else
            {
                var levelWordIds = await _cache.GetWordIdsByLevelAsync(filter.Level, ct);
                query = dbContext.Words
                    .AsNoTracking()
                    .Where(w => levelWordIds.Contains(w.Id));
            }

            if (filter.Difficulty > 0)
                query = query.Where(w => w.Difficulty == filter.Difficulty);

            if (filter.IsMarked != 0)
            {
                var userBookmarks = await _cache.GetUserBookmarksAsync(userId, ct);
                if (filter.IsMarked == 1)
                    query = query.Where(w => userBookmarks.Contains(w.Id));
                else
                    query = query.Where(w => !userBookmarks.Contains(w.Id));
            }

            return query;
        }

        public async Task<IEnumerable<CardDto>> GetCardsListAsync(DeckFilterDto filter, Guid userId, CancellationToken ct)
        {
            var list = await GetCardsListCachedAsync(filter, userId, ct);

            var userBookmarks = await _cache.GetUserBookmarksAsync(userId, ct);
            if (filter.IsMarked != 0)
            {
                if (filter.IsMarked == 1)
                {
                    return list
                        .Where(card => userBookmarks.Contains(card.Id))
                        .Select(card => card.Mark());
                }
                else
                {
                    return list
                        .Where(card => !userBookmarks.Contains(card.Id));
                }
            }
            else
            {
                return list
                    .Select(card =>
                    {
                        if (userBookmarks.Contains(card.Id))
                            return card.Mark();
                        else
                            return card;
                    });
            }
        }
        public async Task<IEnumerable<CardDto>> GetCardsActivityListAsync(DeckFilterDto filter, Guid userId, CancellationToken ct)
        {
            return await GetCardsListCachedAsync(filter, userId, ct);
        }

        private async Task<IEnumerable<CardDto>> GetCardsListCachedAsync(DeckFilterDto filter, Guid userId, CancellationToken ct)
        {
            IEnumerable<CardDto> list;

            if (filter.ThemeId > 0)
                list = await _cache.GetWordsByThemeAsync(filter.ThemeId, ct);
            else
                list = await _cache.GetWordsByLevelAsync(filter.Level, ct);

            if (filter.Difficulty > 0)
                list = list.Where(w => w.Difficulty == filter.Difficulty);

            var userBookmarks = await _cache.GetUserBookmarksAsync(userId, ct);
            if (filter.IsMarked != 0)
            {
                if (filter.IsMarked == 1)
                    list = list.Where(w => userBookmarks.Contains(w.Id));
                else
                    list = list.Where(w => !userBookmarks.Contains(w.Id));
            }

            return list;
        }
    }
}