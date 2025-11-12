using Application.Abstractions.Caching;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.UseCases;
using Domain.Entities.Auth;
using GatewayApi.Services.Background;
using Infrastructure;
using Infrastructure.Caching;
using Infrastructure.DataContexts;
using Infrastructure.Repositories;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Auth.Tokens;
using Infrastructure.Services.FileStorage;
using Infrastructure.Services.Migration;
using Infrastructure.UseCases;
using Microsoft.EntityFrameworkCore;
using Shared;
using StackExchange.Redis;

namespace GatewayApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext registration with Npgsql (PostgreSQL) provider
            var connectionString = configuration.GetConnectionString(SharedConstants.FlashCardsConnectionString);
            services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connectionString));
            return services;
        }
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileStorageService, FileStorageService>();
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<ITokenGenerator<string>, JwtAccessTokenGenerator>();
            services.AddScoped<ITokenGenerator<RefreshToken>, JwtRefreshTokenGenerator>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IWordQueryBuilder, WordQueryBuilder>();
            services.AddScoped<IWordService, WordService>();
            services.AddScoped<IActivityService, ActivityService>();

            return services;
        }

        public static void AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            var redisHost = configuration[SharedConstants.Redis_Host];
            var redisPort = configuration[SharedConstants.Redis_Port];
            var redisPassword = configuration[SharedConstants.Redis_Password];
            var redisDb = configuration.GetValue<int>(SharedConstants.Redis_Database, 0);

            var redisConnectionString = $"{redisHost}:{redisPort},password={redisPassword},defaultDatabase={redisDb}";

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = SharedConstants.Redis_InstanceName;

                var config = ConfigurationOptions.Parse(options.Configuration);
                config.AbortOnConnectFail = false;        // crucial for Prod
                config.ConnectTimeout = 3000;             // 3 sec to connect
                config.SyncTimeout = 3000;                // 3 sec for sync operations
                config.AsyncTimeout = 3000;               // 3 sec for async operations
                config.ConnectRetry = 5;                  // 5 times try to reconnect
                config.ReconnectRetryPolicy = new ExponentialRetry(500, 5000); // backoff

                options.ConfigurationOptions = config;
            });

            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<IWordCacheService, RedisWordCacheService>();
        }

        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<RefreshTokenCleanupService>();
            return services;
        }
    }
}
