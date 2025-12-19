using Application.Abstractions.Repositories;
using Application.Abstractions.Services;
using Application.DTO.Tokens;
using Application.Security;
using Application.UseCases;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using Infrastructure.DataContexts;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Services.Auth;
using Infrastructure.Services.Auth.Tokens.Generators;
using Infrastructure.Services.Background;
using Infrastructure.Services.EmailSender;
using Infrastructure.Services.FileStorage;
using Infrastructure.Services.Migration;
using Infrastructure.Services.RazorRenderer;
using Infrastructure.UseCases;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Configuration;

namespace GatewayApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApiOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ApiOptions>(configuration.GetSection(SharedConstants.EnvApiGroup));
        }

        public static void AddDataContext(this IServiceCollection services, IConfiguration configuration)
        {
            // DbContext registration with Npgsql (PostgreSQL) provider
            var connectionString = configuration.GetConnectionString(SharedConstants.FlashCardsConnectionString);
            services.AddDbContextFactory<DataContext>(options => options.UseNpgsql(connectionString));
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

            services.AddSingleton<IEmailQueue, EmailQueue>();
            services.AddHostedService<EmailQueueBackgroundService>();
        }

        public static void AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddSingleton<IFileStorageService, FileStorageService>();
            services.AddSingleton<IFileStorageService, FileStorageService>();
            services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

            services.AddScoped<ITokenGenerator<string>, JwtConfirmationTokenGenerator>();
            services.AddScoped<ITokenGenerator<string>, JwtResetPasswordTokenGenerator>();
            services.AddScoped<ITokenGenerator<string>, JwtAccessTokenGenerator>();
            services.AddScoped<ITokenGenerator<RefreshToken>, JwtRefreshTokenGenerator>();

            services.AddScoped<IResetPasswordTokenRepository, ResetPasswordTokenRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IJwtTokenReader, JwtTokenReader>();

            services.AddScoped<IUserPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserEmailService, UserEmailService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IWordQueryBuilder, WordQueryBuilder>();
            services.AddScoped<IWordService, WordService>();
            services.AddScoped<IActivityService, ActivityService>();
        }
    }
}