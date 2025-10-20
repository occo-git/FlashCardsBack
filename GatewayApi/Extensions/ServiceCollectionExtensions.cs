using Application.Services;
using Application.Services.Contracts;
using Application.Services.Tokens;
using Domain.Entities.Auth;
using GatewayApi.Services.Background;
using Infrastructure;
using Infrastructure.DataContexts;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Contracts;
using Infrastructure.Services;
using Infrastructure.Services.Contracts;
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
            services.AddScoped<IWordService, WordService>();

            return services;
        }

        public static IServiceCollection AddHostedServices(this IServiceCollection services)
        {
            services.AddHostedService<RefreshTokenCleanupService>();
            return services;
        }
    }
}
