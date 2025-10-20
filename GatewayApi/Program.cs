using Application.Extentions;
using GatewayApi.Extensions;
using Infrastructure;
using Infrastructure.Services;
using Infrastructure.Services.Contracts;
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

#region ProblemDetails (RFC 7807)
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // Add a traceId to the response
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Detail ??= "An error occurred. Please try again.";

        context.ProblemDetails.Title = context.HttpContext.Response.StatusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
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

#region Middleware
// custom middleware can be added here ...
app.UseRouting();
app.UseHttpsRedirection();
app.UseCors(CONST_CorsPolicy);
app.UseStatusCodePages(); // middleware to return status code pages for HTTP errors 

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // detailed error pages in development
    app.UseSwagger(); // generate Swagger as a JSON endpoint
    app.UseSwaggerUI(); // Swagger UI at /swagger
    app.MapGet("/", () => Results.Redirect("/swagger")); // redirect from "/" to "/swagger"
}
else
{
    app.UseExceptionHandler(); // generic error handler in production
    app.UseStatusCodePages(); // middleware to return status code pages for HTTP errors
}

app.UseAuthentication();
app.UseAuthorization();
#endregion

app.MapControllers();

await app.RunAsync();