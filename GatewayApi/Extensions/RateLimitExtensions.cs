using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Polly;
using Shared;
using Shared.Configuration;
using System.Threading.RateLimiting;

namespace GatewayApi.Extensions
{
    public static class RateLimitExtensions
    {
        public static void AddRateLimiter(this IServiceCollection services, RateLimitOptions rlo)
        {
            if (rlo?.Enabled == true)
            {
                services.AddRateLimiter(options =>
                {
                    // http status code and detail when rate limit exceeded
                    options.OnRejected = async (context, ct) =>
                    {
                        var title = "Too Many Requests";
                        var timeLeft = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan w) ? 
                            (w.Hours > 0 ? $"{w.Hours}h" : w.Minutes > 0 ? $"{w.Minutes}m" : $"{w.Seconds}s") : 
                            String.Empty;
                        var detail = $"Too many requests. Try again later{(string.IsNullOrEmpty(timeLeft) ? "" : $" ({timeLeft})")}";

                        var problem = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = title,
                            Detail = detail
                        };
                        context.HttpContext.Response.ContentType = "application/json";
                        await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
                    }
                ;

                    // 1. Global rate limit by IP
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: ip,
                            factory: _ => GetOptions(rlo.GeneralPermitLimit, TimeSpan.FromMinutes(1))); // 300 / 1 minute
                    });

                    // 2. Rate limit policy for Auth endpoints
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitAuthPolicy, o => o.SetOptions(rlo.AuthPermitLimit, TimeSpan.FromMinutes(1))); // 10 / 1 minute

                    // 3. Rate limit policies for Username change, Password change, Delete profile operation
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitUpdateUsernamePolicy, o => o.SetOptions(rlo.UpdateUsernamePermitLimit, TimeSpan.FromHours(1))); // 5 / 1 hour
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitUpdatePasswordPolicy, o => o.SetOptions(rlo.UpdatePasswordPermitLimit, TimeSpan.FromHours(1))); // 3 / 1 hour
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitDeleteProfilePolicy, o => o.SetOptions(rlo.DeleteProfilePermitLimit, TimeSpan.FromHours(24))); // 1 / 24 hours
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitResetPasswordRequestPolicy, o => o.SetOptions(rlo.ResetPasswordRequestPermitLimit, TimeSpan.FromHours(1))); // 10 / 1 hours
                    options.AddFixedWindowLimiter(SharedConstants.RateLimitResetPasswordPolicy, o => o.SetOptions(rlo.ResetPasswordPermitLimit, TimeSpan.FromHours(1))); // 20 / 1 hours
                });
            }
        }

        private static FixedWindowRateLimiterOptions GetOptions(int limit, TimeSpan window)
        {
            return new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,      // requests maximum
                Window = window,          // per window
                QueueLimit = 0,           // no queue, 429 immediately
                AutoReplenishment = true  // refreshes window automaticaly
            };
        }

        private static void SetOptions(this FixedWindowRateLimiterOptions o, int limit, TimeSpan window)
        {
            o.PermitLimit = limit;      // requests maximum
            o.Window = window;          // per window
            o.QueueLimit = 0;           // no queue, 429 immediately
            o.AutoReplenishment = true;  // refreshes window automaticaly
        }


    }
}
