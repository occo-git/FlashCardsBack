using Application.DTO.Words;
using Application.Mapping;
using Application.Services.Contracts;
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

namespace Application.Services
{
    public class WordService : IWordService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger _logger;

        public WordService(
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<WordService> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CardDto?> GetCardById(long wordId, CancellationToken ct)
        {
            _logger.LogInformation("GetWordById: WordId = {WordId}", wordId);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var word = await dbContext.Words
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wordId, ct);

            return word.ToCardDto();
        }

        public async IAsyncEnumerable<CardDto?> GetCards(CardsPageRequestDto request, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetCards: {request}", request);
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var words = GetWords(dbContext, request, ct);
            await foreach (var word in words.WithCancellation(ct))
                yield return word.ToCardDto();
        }

        public async IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, [EnumeratorCancellation] CancellationToken ct)
        {
            _logger.LogInformation("GetWords: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var words = GetWords(dbContext, request, ct);
            await foreach (var word in words.WithCancellation(ct))
            {
                Console.WriteLine(word);
                yield return word.ToWordDto();
            }
        }

        private async IAsyncEnumerable<Word> GetWords(DataContext dbContext, CardsPageRequestDto request, [EnumeratorCancellation] CancellationToken ct)
        {
            var query = GetQuery(dbContext, request.Filter);

            if (request.isDirectionForward)
            {
                // go forward: WordId - id of the first element on the page
                query = query
                    .Where(w => w.Id > request.WordId)
                    .OrderBy(w => w.Id)
                    .Take(request.PageSize);
                Console.WriteLine(query.ToQueryString());
                
                await foreach (var word in query.AsAsyncEnumerable().WithCancellation(ct))
                    yield return word;
            }
            else
            {
                // go back: WordId - id of the last element on the page
                query = query
                    .Where(w => w.Id < request.WordId)
                    .OrderByDescending(w => w.Id)
                    .Take(request.PageSize);
                Console.WriteLine(query.ToQueryString());

                // stack to reverse elements
                var stack = new Stack<Word>();
                await foreach (var word in query.AsAsyncEnumerable().WithCancellation(ct))
                    stack.Push(word);

                while (stack.Count > 0)
                    yield return stack.Pop();
            }
        }

        public async Task<CardExtendedDto?> GetCardWithNeighbors(CardRequestDto request, CancellationToken ct)
        {
            _logger.LogInformation("GetCardWithNeighbors: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var filtered = GetQuery(dbContext, request.Filter);

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
                Console.WriteLine(theme);
                yield return theme;
            }
        }

        private IQueryable<Word> GetQuery(DataContext dbContext, DeckFilterDto filter)
        {
            var query = dbContext.Words.AsNoTracking().Where(w => w.Level == filter.Level);
            if (filter.IsMarked != 0)
                query = query.Where(w => (w.Mark ? 1 : -1) == filter.IsMarked);
            if (filter.ThemeId > 0)
                query = query.Where(w => w.WordThemes.Any(t => t.ThemeId == filter.ThemeId));
            if (filter.Difficulty > 0)
                query = query.Where(w => w.Difficulty == filter.Difficulty);
            return query;
        }
    }
}
