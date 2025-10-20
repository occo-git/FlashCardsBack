using GatewayApi.Auth;
using GatewayApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared;
using Shared.Configuration;
using System.Text;

namespace GatewayApi.Extensions
{
    public static class AuthExtensions
    {
        public static void AddJwtAuthenticationOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtValidationOptions>(configuration.GetSection(SharedConstants.JwtValidationOptions));
            services.Configure<ApiTokenOptions>(configuration.GetSection(SharedConstants.ApiTokenOptions));

            services.AddScoped<CustomJwtBearerEvents>();

            // get the JWT signing key from the environment variable
            var signingKeyString = configuration[SharedConstants.JwtSecretEnv];
            if (string.IsNullOrWhiteSpace(signingKeyString))
                throw new Exception("JWT Signing Key is not set");
 
            // Symmetric key to validate the token
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyString));
            services.AddSingleton(signingKey);
        }

        public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwtBearerOption =>
                {
                    var options = configuration.GetSection(SharedConstants.JwtValidationOptions).Get<JwtValidationOptions>();
                    if (options == null)
                        throw new ArgumentNullException(nameof(options), "JwtValidationOptions cannot be null.");

                    var sp = services.BuildServiceProvider();
                    jwtBearerOption.Events = sp.GetRequiredService<CustomJwtBearerEvents>();

                    var sKey = sp.GetRequiredService<SymmetricSecurityKey>();
                    jwtBearerOption.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = options.ValidateIssuer,
                        ValidateAudience = options.ValidateAudience,
                        ValidateLifetime = options.ValidateLifetime,
                        ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
                        IssuerSigningKey = sKey
                    };
                });
        }
    }
}
