using Application.Abstractions.Caching;
using Application.DTO.Words;
using Application.Mapping;
using Application.UseCases;
using Domain.Entities;
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

            Word? currentCard;
            if (request.WordId == 0)
            {
                currentCard = await filtered.OrderBy(w => w.Id).FirstOrDefaultAsync(ct);
                if (currentCard == null)
                    return null;
            }
            else
            {
                currentCard = await filtered.FirstOrDefaultAsync(w => w.Id == request.WordId, ct);
                if (currentCard == null)
                    return null;
            }

            var previousCard = await filtered
                .Where(w => w.Id < currentCard.Id)
                .OrderByDescending(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefaultAsync(ct);

            var nextCard = await filtered
                .Where(w => w.Id > currentCard.Id)
                .OrderBy(w => w.Id)
                .Select(w => w.ToCardInfo())
                .FirstOrDefaultAsync(ct);

            int currentIndex = 1 + await filtered.CountAsync(w => w.Id < currentCard.Id, ct);
            int total = await filtered.CountAsync(ct);

            return new CardExtendedDto(
                currentCard.ToCardDto(),
                previousCard,
                nextCard,
                currentIndex,
                total);
        }

        public async Task<CardDto?> ChangeMark(long wordId, CancellationToken ct)
        {
            _logger.LogInformation("ChangeMark: WordId = {WordId}", wordId);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var word = await dbContext.Words
                .FirstOrDefaultAsync(w => w.Id == wordId, ct);

            if (word != null)
            {
                word.Mark = !word.Mark;
                await dbContext.SaveChangesAsync();
            }

            return word.ToCardDto();
        }

        public async IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetWords: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var words = GetWords(dbContext, request, userId, ct);
            await foreach (var word in words.WithCancellation(ct))
            {
                //Console.WriteLine(word);
                yield return word.ToWordDto();
            }
        }

        private async IAsyncEnumerable<Word?> GetWords(DataContext dbContext, CardsPageRequestDto request, Guid userId, [EnumeratorCancellation] CancellationToken ct)
        {
            var query = _wordQueryBuilder.BuildQuery(dbContext, request.Filter, userId);

            if (request.isDirectionForward)
            {
                // previous one
                var prev = await query
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .FirstOrDefaultAsync(ct);
                yield return prev;

                // main query
                var pageQuery = query
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Take(request.PageSize);

                await foreach (var word in pageQuery.AsAsyncEnumerable().WithCancellation(ct))
                    yield return word;

                // next one
                var next = await query
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefaultAsync(ct);
                yield return next;
            }
            else
            {
                // next one
                var next = await query
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .FirstOrDefaultAsync(ct);
                yield return next;

                // main query
                var pageQuery = query
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Take(request.PageSize);

                var stack = new Stack<Word>();
                await foreach (var word in pageQuery.AsAsyncEnumerable().WithCancellation(ct))
                    stack.Push(word);

                while (stack.Count > 0)
                    yield return stack.Pop();

                // previous one
                var prev = await query
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Skip(request.PageSize)
                    .FirstOrDefaultAsync(ct);
                yield return prev;
            }
        }
    }
}
