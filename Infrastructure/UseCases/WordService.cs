using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
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
        private readonly IRedisWordCacheService _wordCacheService;
        private readonly IWordQueryBuilder _wordQueryBuilder;
        private readonly ILogger _logger;

        public WordService(
            IDbContextFactory<DataContext> dbContextFactory,
            IRedisWordCacheService wordCacheService,
            IWordQueryBuilder wordQueryBuilder,
            ILogger<WordService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(wordCacheService, nameof(wordCacheService));
            ArgumentNullException.ThrowIfNull(wordQueryBuilder, nameof(wordQueryBuilder));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _wordCacheService = wordCacheService;
            _wordQueryBuilder = wordQueryBuilder;
            _logger = logger;
        }

        public async Task<CardDto?> GetCardById(long wordId, CancellationToken ct)
        {
            _logger.LogInformation("GetWordById: WordId = {WordId}", wordId);

            //var cachedWord = await _cacheService.GetWordAsync(wordId);
            //if (cachedWord != null)
            //    return cachedWord;

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var word = await dbContext.Words
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wordId, ct);

            var dto = word.ToCardDto();
            if (dto == null)
                return null;           

            //await _cacheService.SetWordAsync(dto);
            return dto;
        }
        public async IAsyncEnumerable<ThemeDto?> GetThemes(LevelFilterDto filter, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetThemes: {filter}", filter);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var themes = await dbContext.Themes
                .AsNoTracking()
                .Where(t => t.WordThemes.Any(wt => wt.Word != null && wt.Word.Level == filter.Level))
                .Select(t => t.ToDto(t.WordThemes.Count(wt => wt.Word != null && wt.Word.Level == filter.Level)))
                .ToListAsync(ct);
            foreach (var theme in themes)
                yield return theme;
        }

        public async Task<CardExtendedDto?> GetCardWithNeighbors(CardRequestDto request, Guid userId, CancellationToken ct)
        {
            _logger.LogInformation("GetCardWithNeighbors: {request}", request);

            var filtered = await _wordQueryBuilder.GetCardsListAsync(request.Filter, userId, ct);

            CardDto? current;
            if (request.WordId == 0)
                current = filtered.OrderBy(c => c.Id).FirstOrDefault();
            else
                current = filtered.FirstOrDefault(c => c.Id == request.WordId);                
            if (current == null) return null;

            var previousCard = filtered
                .Where(w => w.Id < current.Id)
                .OrderByDescending(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefault();

            var nextCard = filtered
                .Where(w => w.Id > current.Id)
                .OrderBy(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefault();

            int currentIndex = 1 + filtered.Count(c => c.Id < current.Id);
            int total = filtered.Count();

            return new CardExtendedDto(
                current,
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
            await _wordCacheService.InvalidateBookmarksAsync(userId, ct); // invalidate bookmark cache for user
        }

        public async IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetWords: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var cards = GetCardsPage(request, userId, ct);

            await foreach (var card in cards.WithCancellation(ct))
                yield return card.ToWordDto();
        }

        private async IAsyncEnumerable<CardDto?> GetCardsPage(CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            var filtered = await _wordQueryBuilder.GetCardsListAsync(request.Filter, userId, ct);

            if (request.isDirectionForward)
            {
                // previous one
                var prev = filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefault();
                yield return prev;

                // main query
                var pageQuery = filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Take(request.PageSize);

                foreach (var card in pageQuery)
                    yield return card;

                // next one
                var next = filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefault();
                yield return next;
            }
            else
            {
                // previous one
                var prev = filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefault();
                yield return prev;

                // main query
                var pageQuery = filtered
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Take(request.PageSize);

                var stack = new Stack<CardDto>();
                foreach (var card in pageQuery)
                    stack.Push(card);

                while (stack.Count > 0)
                    yield return stack.Pop();
                             
                // next one
                var next = filtered
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .FirstOrDefault();
                yield return next;
            }
        }
    }
}