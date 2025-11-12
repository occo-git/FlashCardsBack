using Application.Abstractions.Services;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Migration
{
    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly DataContext _dbContext;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(DataContext dbContext, ILogger<DatabaseMigrationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task MigrateDatabaseAsync(CancellationToken ct = default)
        {
            Console.WriteLine("Migration STARTED");
            try
            {
                if (!await _dbContext.Database.GetService<IRelationalDatabaseCreator>().HasTablesAsync(ct))
                {
                    await _dbContext.Database.EnsureCreatedAsync(ct); // Create the database if it doesn't exist
                }
                await _dbContext.Database.MigrateAsync(ct); // Apply any pending migrations
                Console.WriteLine("Migration COMPLETED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration ERROR: {ex}");
                throw;
            }
        }
    }
}
