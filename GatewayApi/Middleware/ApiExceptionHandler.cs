using Application.DTO;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Middleware
{
    public class ApiExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(
            RequestDelegate next, 
            ILogger<ApiExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Clear();
                    context.Response.ContentType = "application/json";

                    context.Response.StatusCode = ex switch
                    {
                        ApplicationException => StatusCodes.Status400BadRequest,
                        UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                        KeyNotFoundException => StatusCodes.Status404NotFound,
                        OperationCanceledException => StatusCodes.Status408RequestTimeout,
                        ValidationException => StatusCodes.Status422UnprocessableEntity,
                        _ => StatusCodes.Status500InternalServerError
                    };

                    await context.Response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Type = ex.GetType().Name,
                            Title = "An error occurred.",
                            Detail = ex.Message,
                            //StackTrace = ex.StackTrace
                        });
                }
            }
        }
    }
}
