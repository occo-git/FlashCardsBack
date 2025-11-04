using Application.DTO.Activity;
using Application.Mapping;
using Application.Services.Contracts;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IWordQueryBuilder _wordQueryBuilder;
        private readonly ILogger _logger;

        public ActivityService(
            IDbContextFactory<DataContext> dbContextFactory,
            IWordQueryBuilder wordQueryBuilder,
            ILogger<ActivityService> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _wordQueryBuilder = wordQueryBuilder ?? throw new ArgumentNullException(nameof(wordQueryBuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<QuizResponseDto> GetQuiz(ActivityRequestDto request, CancellationToken ct)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var query = _wordQueryBuilder.BuildQuery(dbContext, request.Filter);

            // group by PartOfSpeech
            var posGroups = await query
                .GroupBy(w => w.PartOfSpeech)
                .Select(g => new
                {
                    PartOfSpeech = g.Key,
                    Words = g.ToList(),
                    Count = g.Count()
                })
                .Where(g => g.Count >= request.Count) // only those POS, where words count >= Count
                .ToListAsync();

                //
            // no suitable group - throw an error
            if (!posGroups.Any())
                throw new InvalidOperationException("Not enough words match the filter.");

            // random POS
            var random = new Random();
            var selectedGroup = posGroups[random.Next(posGroups.Count)];

            // 4 random words from group
            var selectedWords = selectedGroup.Words
                .Select(w => w.ToWordDto())
                .OrderBy(w => Guid.NewGuid())
                .Take(4)
                .ToArray();

            return new QuizResponseDto(selectedWords!);
        }
    }
}