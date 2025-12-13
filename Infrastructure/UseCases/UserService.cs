using Application.Abstractions.Caching;
using Application.DTO.Activity;
using Application.DTO.Users;
using Application.Exceptions;
using Application.Mapping;
using Application.Security;
using Application.UseCases;
using Application.Validators.Entity;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Users;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Auth;
using Shared.Configuration;
using System.Runtime.CompilerServices;

namespace Infrastructure.UseCases
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly IUserPasswordHasher _passwordHasher;
        private readonly IUserCacheService _userCache;
        private readonly ILogger<UserService> _logger;
        private readonly ApiOptions _apiOptions;

        public UserService(
            IDbContextFactory<DataContext> dbContextFactory,
            IUserPasswordHasher passwordHasher,
            IUserCacheService userCache,
            IOptions<ApiOptions> apiOptions,
            ILogger<UserService> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(passwordHasher, nameof(passwordHasher));
            ArgumentNullException.ThrowIfNull(userCache, nameof(userCache));
            ArgumentNullException.ThrowIfNull(apiOptions, nameof(apiOptions));
            ArgumentNullException.ThrowIfNull(apiOptions.Value, nameof(apiOptions.Value));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _passwordHasher = passwordHasher;
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

        public async Task<User?> GetByNameOrEmailAsync(string? text, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == text || u.UserName == text, ct);
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

        public async Task<User> GetOrAddGoogleUserAsync(string googleEmail, string googleName, CancellationToken ct)
        {
            var existingUser = await GetByEmailAsync(googleEmail, ct);
            if (existingUser != null)
                return existingUser;

            // unique name
            var userName = $"{(String.IsNullOrEmpty(googleName) ? googleEmail.Split('@')[0] : googleName.Replace(" ", "").ToLower())}_{Guid.NewGuid().ToString("N")[..8]}";
            var randomPassword = UserMapper.GenerateRandomPassword(32);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword);

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName, 
                Email = googleEmail,
                PasswordHash = passwordHash,
                Level = Levels.A1,  
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                Provider = Providers.ProviderGoogle,
                EmailConfirmed = true,
                Active = true
            };
            return await AddAsync(newUser, ct);
        }

        public async Task<User> AddAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
            return user;
        }

        public async Task<int> UpdateAsync(User user, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            user.LastActive = DateTime.UtcNow;
            context.Users.Update(user);
            var result = await context.SaveChangesAsync(ct);

            await _userCache.RemoveByIdAsync(user.Id, ct);
            return result;
        }

        public async Task<int> UpdateUsernameAsync(UpdateUsernameDto request, Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var existedUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == request.NewUsername && u.Id != userId, ct);
            if (existedUser != null)
                throw new UserAlreadyExistsException("User with the same username already exists");

            var user = await GetByIdAsync(userId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            if (user.UserName != request.NewUsername)
            {
                user.UserName = request.NewUsername;
                return await UpdateAsync(user, ct);
            }
            return 0;
        }

        public async Task<int> UpdatePasswordAsync(UpdatePasswordDto request, Guid userId, CancellationToken ct)
        {
            if (request.NewPassword == request.OldPassword)
                return 0;

            var user = await GetByIdAsync(userId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            if (!_passwordHasher.VerifyHashedPassword(user.PasswordHash, request.OldPassword))
                throw new UnauthorizedAccessException("Incorrect old password.");

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            return await UpdateAsync(user, ct);
        }

        public async Task<int> DeleteProfileAsync(DeleteProfileDto request, Guid userId, CancellationToken ct)
        {
            var user = await GetByIdAsync(userId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            if (!user.Active && user.IsDeleted)
                return 0;

            user.Active = false;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            return await UpdateAsync(user, ct);
        }

        #region User Level
        public async Task<int> SetLevel(Guid userId, string level, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var user = await GetByIdAsync(userId, ct);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            user.Level = level;
            return await UpdateAsync(user, ct);
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