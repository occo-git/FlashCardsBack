using Application.Abstractions.Caching;
using Application.Abstractions.DataContexts;
using Application.Abstractions.Services;
using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Application.Exceptions;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IUserCacheService _userCache;
        private readonly ILogger<UserService> _logger;
        private readonly ApiOptions _apiOptions;

        public UserService(
            IDbContextFactory<DataContext> dbContextFactory,
            IUserCacheService userCache,
            IOptions<ApiOptions> apiOptions,
            ILogger<UserService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(userCache, nameof(userCache));
            ArgumentNullException.ThrowIfNull(apiOptions, nameof(apiOptions));
            ArgumentNullException.ThrowIfNull(apiOptions.Value, nameof(apiOptions.Value));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _userCache = userCache;
            _apiOptions = apiOptions.Value;
            _logger = logger;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            // Cache-Aside for User
            var cached = await _userCache.GetByIdAsync(id, ct);
            if (cached != null) return cached;

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user != null)
                await _userCache.SetAsync(user, ct);

            return user;
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username, ct);
        }

        public async Task<User?> GetByEmailAsync(string? email, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .AsNoTracking()
                .ToListAsync(ct); // all users in memory
        }

        public async IAsyncEnumerable<User?> GetAllAsyncEnumerable([EnumeratorCancellation] CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var users = context.Users
                .AsNoTracking()
                .AsAsyncEnumerable();

            await foreach (var user in users.WithCancellation(ct)) // users as async enumerable, available for streaming one by one
                yield return user;
        }

        public async Task<User> CreateNewAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            var existingUsername = await GetByUsernameAsync(user.UserName, ct);
            if (existingUsername != null)
                throw new UserAlreadyExistsException("User with the same username already exists");
            var existingEmail = await GetByEmailAsync(user.Email, ct);
            if (existingEmail != null)
                throw new UserAlreadyExistsException("User with the same email already exists");

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.Active = true;

            return user;
        }

        public async Task<User> AddAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
            return user;
        }

        public async Task<User> UpdateAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Users.Update(user);
            await context.SaveChangesAsync(ct);

            await _userCache.RemoveByIdAsync(user.Id, ct);
            return user;
        }

        #region User Level
        public async Task<int> SetLevel(Guid userId, string level, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            existingUser.Level = level;
            var result = await context.SaveChangesAsync(ct);

            await _userCache.RemoveByIdAsync(existingUser.Id, ct);
            return result;
        }
        #endregion

        #region User Progress
        public async Task<ProgressResponseDto> GetProgress(Guid userId, CancellationToken ct)
        {
            await using var context = _dbContextFactory.CreateDbContext();
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            var progressList = await context.UserWordsProgress
                .AsNoTracking()
                .Include(up => up.Word)
                .ThenInclude(w => w!.WordThemes)
                .ThenInclude(wt => wt.Theme)
                .Where(up => up.UserId == userId)
                .ToListAsync(ct);

            // Total Summary across all cards
            var totalSummaryGroups =
                new[] {
                    new ProgressSummaryGroup
                    (
                        Name: "All Cards",
                        Key: "Total",
                        CorrectCount: progressList.Sum(p => p.CorrectCount),
                        TotalAttempts: progressList.Sum(p => p.TotalAttempts)
                    )
                };

            // Summarize by Activity Type
            var activitySummaryGroups = progressList
                .GroupBy(p => p.ActivityType ?? "Unknown")
                .OrderBy(g =>
                    ActivityTypes.ActivityTypeOrder.TryGetValue(g.Key, out var index) ? index : int.MaxValue
                )
                .Select(g => new ProgressSummaryGroup
                (
                    Name: "By Activity",
                    Key: g.Key,
                    CorrectCount: g.Sum(p => p.CorrectCount),
                    TotalAttempts: g.Sum(p => p.TotalAttempts)
                ));

            // Summarize by Level
            var levelSummaryGroups = progressList
                .GroupBy(p => p.Word?.Level ?? "Unknown")
                .Select(g => new ProgressSummaryGroup
                (
                    Name: "By Level",
                    Key: g.Key,
                    CorrectCount: g.Sum(p => p.CorrectCount),
                    TotalAttempts: g.Sum(p => p.TotalAttempts)
                ))
                .OrderByDescending(g => g.Key);

            return new ProgressResponseDto(
                totalSummaryGroups
                    .Union(activitySummaryGroups)
                    .Union(levelSummaryGroups)
                    .ToArray()
            );
        }

        public async Task<int> SaveProgress(Guid userId, ActivityProgressRequestDto request, CancellationToken ct)
        {
            await using var context = _dbContextFactory.CreateDbContext();
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            // get existing progress
            var existingProgress = await context.UserWordsProgress
                .Where(p =>
                    p.UserId == userId &&
                    p.ActivityType == request.ActivityType &&
                    p.WordId == request.WordId &&
                    p.FillBlankId == request.FillBlankId)
                .FirstOrDefaultAsync(ct);

            if (existingProgress == null)
            {
                existingProgress = new UserWordsProgress()
                {
                    UserId = userId,
                    ActivityType = request.ActivityType,
                    WordId = request.WordId,
                    FillBlankId = request.FillBlankId,
                    CorrectCount = request.IsSuccess ? 1 : 0,
                    TotalAttempts = 1,
                    SuccessRate = request.IsSuccess ? 1 : 0,
                    LastSeen = DateTime.UtcNow
                };
                await context.UserWordsProgress.AddAsync(existingProgress, ct);
            }
            else
            {
                var correctCount = existingProgress.CorrectCount + (request.IsSuccess ? 1 : 0);
                var totalAttempts = existingProgress.TotalAttempts + 1;
                var successRate = (double)correctCount / totalAttempts;

                existingProgress.CorrectCount = correctCount;
                existingProgress.TotalAttempts = totalAttempts;
                existingProgress.SuccessRate = successRate;
                existingProgress.LastSeen = DateTime.UtcNow;
            }
            return await context.SaveChangesAsync(ct);
        }
        #endregion
    }
}