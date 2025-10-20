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

        public async IAsyncEnumerable<CardDto?> GetCards(GetCardsDto? request, [EnumeratorCancellation] CancellationToken ct)
        {
            if (request == null)
                request = GetCardsDto.Default;

            _logger.LogInformation("GetWords: {request}", request);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var wordsQuery = dbContext.Words
                .AsNoTracking()
                .Where(w => w.Id > request.LastId)
                .Take(request.PageSize)
                .AsAsyncEnumerable();

            await foreach (var word in wordsQuery.WithCancellation(ct))
            {
                yield return word.ToDto();
            }
        }

        public async Task<CardDto?> GetCardById(long wordId, CancellationToken ct)
        {
            _logger.LogInformation("GetWordById: WordId = {WordId}", wordId);

            await using var dbContext = _dbContextFactory.CreateDbContext();
            var word = await dbContext.Words
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wordId, ct);

            return word.ToDto();
        }
    }
}
