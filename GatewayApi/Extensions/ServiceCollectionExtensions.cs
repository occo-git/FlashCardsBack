using Application.Abstractions.Caching;
using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTO.Tokens;
using Application.UseCases;
using Domain.Entities.Auth;
using GatewayApi.Services.Background;
using Infrastructure;
using Infrastructure.Caching;
using Infrastructure.DataContexts;
using Infrastructure.Repositories;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Auth.Tokens;
using Infrastructure.Services.EmailSender;
using Infrastructure.Services.FileStorage;
using Infrastructure.Services.Migration;
using Infrastructure.Services.RazorRenderer;
using Infrastructure.UseCases;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
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

        public static void AddRazorRenderer(this IServiceCollection services)
        {
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddScoped<IRazorRenderer, RazorRenderer>();
        }

        public static void AddEmailSender(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SmtpOptions>(configuration.GetSection(SharedConstants.EnvSmtpGroup));
            services.AddTransient<IEmailSender, EmailSender>();
        }

        public static void AddCache(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RedisOptions>(configuration.GetSection(SharedConstants.EnvRedisGroup));

            var sp = services.BuildServiceProvider();
            var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;

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
            services.AddSingleton<IWordCacheService, RedisWordCacheService>();
        }        
        
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileStorageService, FileStorageService>();
            services.AddSingleton<IFileStorageService, FileStorageService>();
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

            services.AddScoped<ITokenGenerator<ConfirmationTokenDto>, JwtConfirmationTokenGenerator>();
            services.AddScoped<ITokenGenerator<string>, JwtAccessTokenGenerator>();
            services.AddScoped<ITokenGenerator<RefreshToken>, JwtRefreshTokenGenerator>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IWordQueryBuilder, WordQueryBuilder>();
            services.AddScoped<IWordService, WordService>();
            services.AddScoped<IActivityService, ActivityService>();

            return services;
        }

        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<RefreshTokenCleanupService>();
            return services;
        }
    }
}
