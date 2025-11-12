using Application.Abstractions.DataContexts;
using Application.DTO.Activity;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Users;
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
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<UserService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .FirstOrDefaultAsync(u => u.Username == username, ct);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IAsyncEnumerable<User>> GetAllAsyncEnumerable(CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return context.Users.AsAsyncEnumerable();
        }

        public async Task<User> CreateAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            var existingUsername = await GetByUsernameAsync(user.Username, ct);
            if (existingUsername != null)
                throw new ApplicationException("User with the same username already exists");
            var existingEmail = await GetByEmailAsync(user.Email, ct);
            if (existingEmail != null)
                throw new ApplicationException("User with the same email already exists");

            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);

            return user;
        }

        public async Task<User> UpdateAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existingUser = await context.Users.FindAsync(user.Id, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            // uпdate only the necessary fields
            existingUser.Username = user.Username;
            existingUser.PasswordHash = user.PasswordHash;

            await context.SaveChangesAsync(ct);

            return existingUser;
        }

        public async Task<int> SetLevel(Guid userId, string level, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            existingUser.Level = level;
            return await context.SaveChangesAsync(ct);
        }

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
    }
}