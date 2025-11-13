using Application.DTO.Activity;
using Application.DTO.Words;
using Application.Mapping;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases
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
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(wordQueryBuilder, nameof(wordQueryBuilder));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _wordQueryBuilder = wordQueryBuilder;
            _logger = logger;
        }

        public async Task<QuizResponseDto> GetQuiz(ActivityRequestDto request, Guid userId, CancellationToken ct)
        {
            var quizWords = await GetWords(request, userId, ct);
            return new QuizResponseDto(ActivityTypes.Quiz, quizWords!);
        }

        public async Task<TypeWordResponseDto> GetTypeWord(ActivityRequestDto request, Guid userId, CancellationToken ct)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var filtered = _wordQueryBuilder.BuildQuery(dbContext, request.Filter, userId);

            // random word
            var word = await filtered
                .OrderBy(w => Guid.NewGuid())
                .FirstOrDefaultAsync(ct);
            if (word == null)
                throw new InvalidOperationException("Not enough words match the filter.");
            else
                return new TypeWordResponseDto(ActivityTypes.TypeWord, word.ToWordDto());
        }

        public async Task<FillBlankResponseDto> GetFillBlank(ActivityRequestDto request, Guid userId, CancellationToken ct)
        {
            var words = await GetWords(request, userId, ct);

            // random word
            var word = words
                .OrderBy(w => Guid.NewGuid())
                .FirstOrDefault();
            if (word == null)
                throw new InvalidOperationException("Not enough words match the filter.");

            // random fill blank for a word
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var blank = await dbContext.FillBlanks
                .Where(fb => fb.WordId == word.Id)
                .Select(fb => fb.ToFillBlankDto())
                .OrderBy(w => Guid.NewGuid())
                .FirstOrDefaultAsync(ct);
            if (blank == null)
                throw new InvalidOperationException("No fill blank for a word");

            return new FillBlankResponseDto(ActivityTypes.FillBlank, blank, words!);
        }

        private async Task<WordDto[]> GetWords(ActivityRequestDto request, Guid userId, CancellationToken ct)
        {
            await using var dbContext = _dbContextFactory.CreateDbContext();
            var filtered = _wordQueryBuilder.BuildQuery(dbContext, request.Filter, userId);

            // group by PartOfSpeech
            var posGroups = await filtered
                .GroupBy(w => w.PartOfSpeech)
                .Select(g => new
                {
                    PartOfSpeech = g.Key,
                    Words = g.ToList(),
                    Count = g.Count()
                })
                .Where(g => g.Count >= request.Count) // only those POS, where words count >= Count
                .ToListAsync(ct);

            // no suitable group - throw an error
            if (!posGroups.Any())
                throw new InvalidOperationException("Not enough words match the filter.");

            // random POS
            var random = new Random();
            var selectedGroup = posGroups[random.Next(posGroups.Count)];

            // random words from group
            var selectedWords = selectedGroup.Words
                .OrderBy(w => Guid.NewGuid())
                .Take(request.Count)
                .Select(w => w.ToWordDto())
                .ToArray();

            return selectedWords;
        }
    }
}