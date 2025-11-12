using Application.Abstractions.Services;
using Application.Extentions;
using FluentValidation;
using GatewayApi.Extensions;
using Infrastructure;
using Infrastructure.Services.Migration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Shared;

const string CONST_CorsPolicy = "CorsPolicy";

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(options =>
{
    options.AddPolicy(CONST_CorsPolicy, policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:4200") // Angular dev server
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials();
    });
});
#endregion

#region DataContext
builder.Services.AddDataContext(builder.Configuration);
#endregion

#region Cache
builder.Services.AddCache(builder.Configuration);
#endregion

#region Registration
builder.Services.AddControllers();
builder.Services.AddValidators(); // FluentValidation registration
builder.Services.AddOptions(builder.Configuration); // Options registration
builder.Services.AddInfrastructureServices(); // Infrastructure services registration
builder.Services.AddHostedServices(); // Hosted services registration
builder.Services.AddJwtAuthenticationOptions(builder.Configuration); // JWT authentication options registration
builder.Services.AddJwtAuthentication(builder.Configuration); // JWT authentication registration

builder.Services.AddEndpointsApiExplorer(); // Swagger/OpenAPI
builder.Services.AddSwaggerGen(); // SwaggerGen
builder.Services.AddSwaggerGen(c =>
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
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Detail ??= "An error occurred. Please try again.";
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
if (args.Length > 0 && args[0].Equals(InfrastructureConstants.Migrate, StringComparison.InvariantCultureIgnoreCase))
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
                    UnauthorizedAccessException _ => StatusCodes.Status403Forbidden,
                    KeyNotFoundException _ => StatusCodes.Status404NotFound,
                    OperationCanceledException _ => StatusCodes.Status408RequestTimeout,
                    ValidationException _ => StatusCodes.Status422UnprocessableEntity,
                    ApplicationException _ => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status500InternalServerError
                },
                Detail = exception?.Message
            };

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

// ⚠️ middleware to return status code pages for HTTP errors - app.UseStatusCodePages();
app.UseStatusCodePagesWithReExecute("/error/{0}");
#endregion

// Route to process errors by status code
app.MapGet("/error/{code}", (int code, HttpContext context) =>
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