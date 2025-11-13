using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases
{
    public class WordService : IWordService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IWordQueryBuilder _wordQueryBuilder;
        private readonly IWordCacheService _cacheService;
        private readonly ILogger _logger;

        public WordService(
            IDbContextFactory<DataContext> dbContextFactory,
            IWordQueryBuilder wordQueryBuilder,
            IWordCacheService cacheService,
            ILogger<WordService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(wordQueryBuilder, nameof(wordQueryBuilder));
            ArgumentNullException.ThrowIfNull(cacheService, nameof(cacheService));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _wordQueryBuilder = wordQueryBuilder;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<CardDto?> GetCardById(long wordId, CancellationToken ct)
        {
            _logger.LogInformation("GetWordById: WordId = {WordId}", wordId);

            var cachedWord = await _cacheService.GetWordAsync(wordId);
            if (cachedWord != null)
                return cachedWord;

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var word = await dbContext.Words
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wordId, ct);

            var dto = word.ToCardDto();
            if (dto == null)
                return null;           

            await _cacheService.SetWordAsync(dto);
            return dto;
        }
        public async IAsyncEnumerable<ThemeDto?> GetThemes(LevelFilterDto filter, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetThemes: {filter}", filter);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var themes = await dbContext.Themes
                .Where(t => t.WordThemes.Any(wt => wt.Word != null && wt.Word.Level == filter.Level))
                .Select(t => t.ToDto(t.WordThemes.Count(wt => wt.Word != null && wt.Word.Level == filter.Level)))
                .ToListAsync(ct);
            foreach (var theme in themes)
            {
                //Console.WriteLine(theme);
                yield return theme;
            }
        }

        public async Task<CardExtendedDto?> GetCardWithNeighbors(CardRequestDto request, Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("GetCardWithNeighbors: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var filtered = _wordQueryBuilder.BuildQuery(dbContext, request.Filter, userId);

            Word? current;
            if (request.WordId == 0)
                current = await filtered.OrderBy(c => c.Id).FirstOrDefaultAsync(ct);
            else
                current = await filtered.FirstOrDefaultAsync(c => c.Id == request.WordId, ct);                
            if (current == null) return null;

            bool currentIsMarked = await dbContext.UserBookmarks.AnyAsync(b => b.WordId == current.Id && b.UserId == userId);

            var previousCard = await filtered
                .Where(w => w.Id < current.Id)
                .OrderByDescending(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefaultAsync(ct);

            var nextCard = await filtered
                .Where(w => w.Id > current.Id)
                .OrderBy(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefaultAsync(ct);

            int currentIndex = 1 + await filtered.CountAsync(c => c.Id < current.Id, ct);
            int total = await filtered.CountAsync(ct);

            return new CardExtendedDto(
                current.ToCardDto(currentIsMarked),
                previousCard,
                nextCard,
                currentIndex,
                total);
        }

        public async Task ChangeMark(long wordId, Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("ChangeMark: WordId = {WordId}", wordId);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var bookmark = await dbContext.UserBookmarks.FirstOrDefaultAsync(b => b.WordId == wordId && b.UserId == userId, ct);
            if (bookmark != null)
            {                
                dbContext.UserBookmarks.Remove(bookmark);
                await dbContext.SaveChangesAsync(ct);
            }
            else
            {
                var newBookmark = new UserBookmark() { WordId = wordId, UserId = userId };
                dbContext.UserBookmarks.Add(newBookmark);
                await dbContext.SaveChangesAsync(ct);
            }
        }

        public async IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetWords: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var words = GetWords(dbContext, request, userId, ct);

            await foreach (var word in words.WithCancellation(ct))
                yield return word.ToWordDto();
        }

        private async IAsyncEnumerable<(Word?, bool)> GetWords(DataContext dbContext, CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            var filtered = _wordQueryBuilder.BuildQuery(dbContext, request.Filter, userId);

            if (request.isDirectionForward)
            {
                // previous one
                var prev = await filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefaultAsync(ct);
                yield return (prev, false);

                // main query
                var pageQuery = filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Take(request.PageSize)
                    .Include(w => w.Bookmarks.Where(b => b.UserId == userId));

                await foreach (var word in pageQuery.AsAsyncEnumerable().WithCancellation(ct))
                    yield return (word, word.Bookmarks.Any());

                // next one
                var next = await filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefaultAsync(ct);
                yield return (next, false);
            }
            else
            {
                // next one
                var next = await filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .FirstOrDefaultAsync(ct);
                yield return (next, false);

                // main query
                var pageQuery = filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Take(request.PageSize)
                    .Include(w => w.Bookmarks.Where(b => b.UserId == userId));

                var stack = new Stack<(Word, bool)>();
                await foreach (var word in pageQuery.AsAsyncEnumerable().WithCancellation(ct))
                    stack.Push((word, word.Bookmarks.Any()));

                while (stack.Count > 0)
                    yield return stack.Pop();

                // previous one
                var prev = await filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefaultAsync(ct);
                yield return (prev, false);
            }
        }
    }
}
