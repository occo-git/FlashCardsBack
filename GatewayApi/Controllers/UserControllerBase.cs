using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GatewayApi.Controllers
{
    [ApiController]
    public abstract class UserControllerBase : ControllerBase
    {
        protected readonly ILogger<UserControllerBase> _logger;

        protected UserControllerBase(ILogger<UserControllerBase> logger)
        {
            _logger = logger;
        }

        protected async Task<T> GetCurrentUserAsync<T>(Func<Guid, Task<T>> action)
        {
            //var ct = HttpContext?.RequestAborted ?? CancellationToken.None;

            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                //_logger.LogWarning("> GetCurrentUser: User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                //_logger.LogInformation($"> GetCurrentUser UserId = {userId}");
                return await action(userId);
            }
            else
            {
                _logger.LogError("> GetCurrentUser: Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }

        protected T GetCurrentUser<T>(Func<Guid, T> action)
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                //_logger.LogWarning("> GetCurrentUser: User ID claim not found");
                throw new UnauthorizedAccessException("Unauthorized user");
            }

            if (Guid.TryParse(id, out var userId))
            {
                //_logger.LogInformation($"> GetCurrentUser UserId = {userId}");
                return action(userId);
            }
            else
            {
                _logger.LogError("> GetCurrentUser: Invalid user ID format: {Id}", id);
                throw new FormatException($"Invalid user ID format: {id}");
            }
        }
    }
}
