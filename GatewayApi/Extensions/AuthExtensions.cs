using GatewayApi.Auth;
using GatewayApi.Services.Background;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared;
using Shared.Configuration;
using System.Text;

namespace GatewayApi.Extensions
{
    public static class AuthExtensions
    {
        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {            
            services.Configure<JwtValidationOptions>(configuration.GetSection(SharedConstants.JwtValidationOptions));
            services.Configure<ApiTokenOptions>(configuration.GetSection(SharedConstants.ApiTokenOptions));
            services.Configure<AuthOptions>(configuration.GetSection(SharedConstants.EnvAuthGroup));
            services.AddScoped<CustomJwtBearerEvents>();

            services.AddHostedService<RefreshTokenCleanupService>();

            // get the JWT signing key from the environment variable
            var signingKeyString = configuration[SharedConstants.EnvJwtSecret];
            ArgumentException.ThrowIfNullOrWhiteSpace(signingKeyString, nameof(signingKeyString));

            // Symmetric key to validate the token
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyString));
            services.AddSingleton(signingKey);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtBearerOption =>
                {
                    var options = configuration.GetSection(SharedConstants.JwtValidationOptions).Get<JwtValidationOptions>();
                    ArgumentNullException.ThrowIfNull(options, nameof(options));

                    jwtBearerOption.EventsType = typeof(CustomJwtBearerEvents);

                    jwtBearerOption.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = options.ValidateIssuer,
                        ValidateAudience = options.ValidateAudience,
                        ValidateLifetime = options.ValidateLifetime,
                        ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
                        IssuerSigningKey = signingKey
                    };
                });
        }
    }
}