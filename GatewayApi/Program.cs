using Application.Abstractions.Services;
using Application.Exceptions;
using Application.Extentions;
using Application.Mapping;
using FluentValidation;
using GatewayApi.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Shared;

public class Program
{

    const string CONST_CorsPolicy = "CorsPolicy";

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var configuration = builder.Configuration;

        #region Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff UTC ";
            options.UseUtcTimestamp = true;
            options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
            options.IncludeScopes = false;
        });
        #endregion

        #region CORS 

        var apiOptions = services.AddApiOptions(configuration); // api links
        services.AddCors(options =>
        {
            options.AddPolicy(CONST_CorsPolicy, policyBuilder =>
            {
                policyBuilder.WithOrigins(apiOptions.OriginUrl)
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials();
            });
        });
        #endregion

        #region DataContext
        services.AddDataContext(configuration);
        #endregion

        #region Registration
        services.AddControllers();
        services.AddValidators(); // FluentValidation registration
        services.AddRazorRenderer(); // Renders pages into html
        services.AddEmailSender(configuration); // Sends emails
        services.AddCache(configuration); // Cache
        services.AddInfrastructureServices(); // Infrastructure services registration
        services.AddHostedServices(); // Hosted services registration
        services.AddJwtAuthenticationOptions(configuration); // JWT authentication options registration
        services.AddJwtAuthentication(configuration); // JWT authentication registration

        services.AddEndpointsApiExplorer(); // Swagger/OpenAPI
        services.AddSwaggerGen(); // SwaggerGen
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(SharedConstants.ApiVersion,
                new OpenApiInfo
                {
                    Title = SharedConstants.ApiTitle,
                    Version = SharedConstants.ApiVersion
                });
        });
        #endregion

        #region ⚠️ ProblemDetails (RFC 7807)
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Detail ??= "An error occurred. Please try again later.";
                // IHostEnvironment from services
                var env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                if (env.IsDevelopment())
                    // Add a traceId to the response in development
                    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                context.ProblemDetails.Title = context.HttpContext.Response.StatusCode switch
                {
                    StatusCodes.Status400BadRequest => "Bad Request",
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status401Unauthorized => "Unauthorized",
                    StatusCodes.Status403Forbidden => "Forbidden",
                    StatusCodes.Status408RequestTimeout => "Request Timeout",
                    StatusCodes.Status409Conflict => "Conflict",
                    StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
                    StatusCodes.Status500InternalServerError => "Internal Server Error",
                    _ => context.ProblemDetails.Title
                };
            };
        });
        #endregion

        var app = builder.Build();

        #region Migration
        if (args.Length > 0 && args[0].Equals(SharedConstants.Migrate, StringComparison.InvariantCultureIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await migrationService.MigrateDatabaseAsync(cts.Token);

            return; // Application will exit after migration
        }
        #endregion

        #region ⚠️ Exception handling
        //if (app.Environment.IsDevelopment())
        //{
        //    // In development: detaild HTML-page for debuging
        //    app.UseDeveloperExceptionPage();
        //}
        //else
        {
            // In production: intercepts  exceptions and returns secure JSON (Problem Details)
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
                    var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                    var problem = new ProblemDetails
                    {
                        Status = exception switch
                        {
                            ArgumentException _ => StatusCodes.Status400BadRequest,
                            EmailNotConfirmedException => StatusCodes.Status400BadRequest,
                            TokenInvalidFormatException => StatusCodes.Status400BadRequest,
                            AccountNotActiveException _ => StatusCodes.Status403Forbidden,
                            UnauthorizedAccessException _ => StatusCodes.Status403Forbidden,
                            KeyNotFoundException _ => StatusCodes.Status404NotFound,
                            OperationCanceledException _ => StatusCodes.Status408RequestTimeout,
                            ValidationException _ => StatusCodes.Status422UnprocessableEntity,
                            EmailAlreadyConfirmedException => StatusCodes.Status409Conflict,
                            UserAlreadyExistsException _ => StatusCodes.Status409Conflict,
                            ConfirmationLinkMismatchException _ => StatusCodes.Status410Gone,
                            ConfirmationLinkRateLimitException _ => StatusCodes.Status429TooManyRequests,
                            ConfirmationFailedException _ => StatusCodes.Status500InternalServerError,
                            ConfirmationSendFailException => StatusCodes.Status500InternalServerError,
                            System.Net.Mail.SmtpException _ => StatusCodes.Status503ServiceUnavailable,
                            _ => StatusCodes.Status500InternalServerError
                        },
                        Detail = exception switch
                        {
                            ArgumentException ae => ae.Message,
                            EmailNotConfirmedException enc => enc.Message,
                            TokenInvalidFormatException tif => tif.Message,
                            AccountNotActiveException ana => ana.Message,
                            UnauthorizedAccessException u => u.Message,
                            KeyNotFoundException knf => knf.Message,
                            OperationCanceledException oc => oc.Message,
                            ValidationException v => v.Message,
                            EmailAlreadyConfirmedException eac => eac.Message,
                            ConfirmationLinkMismatchException cl => cl.Message,
                            ConfirmationLinkRateLimitException clrl => clrl.Message,
                            UserAlreadyExistsException uae => uae.Message,
                            ConfirmationFailedException cf => cf.Message,
                            ConfirmationSendFailException csf => csf.Message,
                            System.Net.Mail.SmtpException smtpe => smtpe.Message,
                            Exception ex => app.Environment.IsDevelopment() ? ex.Message : "An unexpected error occurred. Please try again later."
                        }
                    };
                    problem.Extensions[ErrorCodeMapper.ErrorCode] = ErrorCodeMapper.Map(exception);

                    // Exception logging
                    //if (exception != null)
                    //{
                    //    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    //    logger.LogError(exception, "Unhandled exception occurred at {Path}", context.Request.Path);
                    //}

                    await problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails = problem
                    });
                });
            });
        }
        #endregion

        #region Middleware
        //app.UseMiddleware<ApiExceptionHandler>(); // ⚠️ Custom exception handling middleware
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseCors(CONST_CorsPolicy);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(); // generate Swagger as a JSON endpoint
            app.UseSwaggerUI(); // Swagger UI at /swagger
            app.MapGet("/", () => Results.Redirect("/swagger")); // redirect from "/" to "/swagger"
        }

        app.UseAuthentication();
        app.UseAuthorization();
        #endregion

        // Route to process errors by status code
        app.MapGet("/Error/{code}", (int code, HttpContext context) =>
        {
            var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
            var problem = new ProblemDetails
            {
                Status = code
            };
            return problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem
            });
        });

        app.MapControllers();

        await app.RunAsync();
    }
}