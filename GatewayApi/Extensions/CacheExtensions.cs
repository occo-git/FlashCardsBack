using Application.Abstractions.Caching;
using GatewayApi.Services.Background;
using Infrastructure.Caching;
using Shared;
using Shared.Configuration;
using StackExchange.Redis;

namespace GatewayApi.Extensions
{
    public static class CacheExtensions
    {
        public static void AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CacheServiceOptions>(configuration.GetSection(SharedConstants.CacheServiceOptions));
            services.Configure<RedisOptions>(configuration.GetSection(SharedConstants.EnvRedisGroup));

            var redisOptions = configuration.GetSection(SharedConstants.EnvRedisGroup).Get<RedisOptions>()!;
            ArgumentNullException.ThrowIfNull(redisOptions);
            var redisConnectionString = $"{redisOptions.Host}:{redisOptions.Port},password={redisOptions.Password},defaultDatabase={redisOptions.Db}";

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = redisOptions.InstanceName;

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
            services.AddSingleton<IRedisDbService, RedisDbService>();
            services.AddSingleton<IRefreshTokenCacheService, RedisRefreshTokenCacheService>();
            services.AddSingleton<IUserCacheService, RedisUserCacheService>();
            services.AddSingleton<IRedisWordCacheService, RedisWordCacheService>();

            services.AddHostedService<CacheRefreshBackgroundService>();
        }

        public static async Task PreloadCache(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                Console.WriteLine("[ Cache ]");

                var redisDbService = scope.ServiceProvider.GetRequiredService<IRedisDbService>();
                Console.WriteLine($" • Flush Db");
                await redisDbService.FlushDb();

                var smartWordCache = scope.ServiceProvider.GetRequiredService<IRedisWordCacheService>();
                await smartWordCache.PreloadAllLevelsAsync(CancellationToken.None);

                Console.WriteLine("[ Cache Info ]");
                Console.WriteLine($" • Database Size = {await redisDbService.GetDatabaseSizeAsync()}");
                Console.WriteLine($"{await redisDbService.GetMemoryInfoAsync()}");
            }
        }
    }
}