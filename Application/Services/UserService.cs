using Application.Services.Contracts;
using Domain.Entities;
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
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<UserService> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (user == null) throw new ArgumentNullException(nameof(user));

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
            if (user == null) throw new ArgumentNullException(nameof(user));

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

        public async Task<bool> DeleteAsync(Guid userId, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FindAsync(userId, ct);
            if (user == null) return false;

            context.Users.Remove(user);
            await context.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> SetLevel(Guid userId, string level, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var existingUser = await context.Users.FindAsync(userId, ct);
            if (existingUser == null)
                throw new KeyNotFoundException("User not found");

            existingUser.Level = level;
            await context.SaveChangesAsync(ct);

            return true;
        }
    }
}