using Shared;
using StackExchange.Redis;

namespace GatewayApi.Extensions
{
    public static class CacheExtensions
    {
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
        }
    }
}
