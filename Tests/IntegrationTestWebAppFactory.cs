using Infrastructure.DataContexts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tests
{
    public class IntegrationTestWebAppFactory
        : WebApplicationFactory<Program>,
          IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:latest")
            .WithDatabase("flashcards_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(SharedConstants.EnvJwtSecret, "test-super-secret-jwt-key-that-is-at-least-32-chars!!");
            builder.UseEnvironment("Development");

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    s => s.ServiceType == typeof(DbContextOptions<DataContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContextFactory<DataContext>(options =>
                    options.UseNpgsql(_dbContainer.GetConnectionString()));
            });
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("--> PostgreSQL container");
            await _dbContainer.StartAsync();

            // Apply migrations
            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
            Console.WriteLine("--> Migration");
            await dbContext.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                );");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("<-- Migration");
        }
        public new Task DisposeAsync() => _dbContainer.StopAsync();
    }
}