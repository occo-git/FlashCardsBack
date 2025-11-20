using Docker.DotNet.Models;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration
{
    public abstract class BaseIntegrationTest<TService> : 
        IClassFixture<IntegrationTestWebAppFactory>, 
        IAsyncLifetime
        where TService : notnull
    {
        private readonly IServiceScope _scope; 
        protected readonly DataContext DbContext;
        protected readonly TService Service;

        protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
        {
            _scope = factory.Services.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<DataContext>();
            Service = _scope.ServiceProvider.GetRequiredService<TService>();
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("--> InitializeAsync");
            await ResetDatabaseAsync();
        }

        private async Task ResetDatabaseAsync()
        {
            Console.WriteLine("--> Resetting database");
            DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
            DbContext.Users.RemoveRange(DbContext.Users);
            await DbContext.SaveChangesAsync();
            Console.WriteLine("<-- Resetting database");
        }

        public async Task InTransactionAsync(Func<DataContext, Task> testLogic)
        {
            Console.WriteLine("--> InTransactionAsync");
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                await testLogic(DbContext);
            }
            finally
            {
                await transaction.RollbackAsync();
                Console.WriteLine("<-- InTransactionAsync");
            }
        }

        public async Task DisposeAsync()
        {
            Console.WriteLine("<-- DisposeAsync");
            if (DbContext != null) 
                await DbContext.DisposeAsync();
            _scope?.Dispose();
        }
    }
}