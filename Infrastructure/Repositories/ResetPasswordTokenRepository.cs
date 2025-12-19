using Application.Abstractions.Repositories;
using Domain.Entities.Users;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ResetPasswordTokenRepository : IResetPasswordTokenRepository
    {
        private readonly IDbContextFactory<DataContext> _dbContextFactory;
        private readonly ILogger<ResetPasswordTokenRepository> _logger;

        public ResetPasswordTokenRepository(
            IDbContextFactory<DataContext> dbContextFactory,
            ILogger<ResetPasswordTokenRepository> logger)
        {
            ArgumentNullException.ThrowIfNull(dbContextFactory, nameof(dbContextFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<int> AddAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct)
        {
            _logger.LogInformation($"Adding reset password token: UserId = {resetPasswordToken.UserId}");
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            await context.ResetPasswordTokens.AddAsync(resetPasswordToken, ct);

            return await context.SaveChangesAsync(ct);
        }

        public async Task<int> UpdateAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct)
        {
            _logger.LogInformation($"Update reset password token: UserId = {resetPasswordToken.UserId}");
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            context.ResetPasswordTokens.Update(resetPasswordToken);
            return await context.SaveChangesAsync(ct);
        }
        public async Task<int> DeleteAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct)
        {
            _logger.LogInformation($"Update reset password token: UserId = {resetPasswordToken.UserId}");
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.ResetPasswordTokens.Where(t => t.Id == resetPasswordToken.Id).ExecuteDeleteAsync(ct);
        }

        public async Task<ResetPasswordToken?> GetByUserIdAsync(Guid userId, CancellationToken ct)
        {
            _logger.LogInformation($"Getting reset password token: UserId = {userId}");
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        }

        public async Task<ResetPasswordToken?> GetAsync(string tokenValue, CancellationToken ct)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(ct);
            return await context.ResetPasswordTokens
                .FirstOrDefaultAsync(t => t.Token == tokenValue, ct);
        }
    }
}
