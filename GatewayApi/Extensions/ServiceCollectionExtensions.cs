using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.UseCases;
using Domain.Entities.Auth;
using GatewayApi.Services.Background;
using Infrastructure;
using Infrastructure.DataContexts;
using Infrastructure.Repositories;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Auth.Tokens;
using Infrastructure.Services.FileStorage;
using Infrastructure.Services.Migration;
using Infrastructure.UseCases;
using Microsoft.EntityFrameworkCore;

namespace GatewayApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext registration with Npgsql (PostgreSQL) provider
            var connectionString = configuration.GetConnectionString(InfrastructureConstants.FlashCardsConnectionString);
            services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connectionString));
            return services;
        }

        public static void AddOptions(this IServiceCollection services, IConfiguration configuration)
        {
            //services.Configure<RefreshTokenCleanupOptions>(configuration.GetSection(ApiConstants.RefreshTokenCleanupOptions));
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

        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<RefreshTokenCleanupService>();
            return services;
        }
    }
}
