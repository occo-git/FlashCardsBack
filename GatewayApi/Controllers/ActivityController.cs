using Application.DTO.Activity;
using Application.DTO.Words;
using Application.UseCases;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/activity")]
    public class ActivityController : UserControllerBase
    {
        private readonly IActivityService _activityService;

        public ActivityController(
            IActivityService activityService,
            ILogger<ActivityController> logger) : base(logger) 
        {
            _activityService = activityService;
        }

        [HttpPost("quiz")]
        [Authorize]
        public async Task<ActionResult<QuizResponseDto>> GetQuiz(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetQuiz {request}");
            
            return await GetCurrentUserAsync(async userId =>
                await _activityService.GetQuiz(request, userId, ct));
        }

        [HttpPost("type-word")]
        [Authorize]
        public async Task<ActionResult<TypeWordResponseDto>> GetTypeWord(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetTypeWord {request}");
            
            return await GetCurrentUserAsync(async userId =>
                await _activityService.GetTypeWord(request, userId, ct));
        }

        [HttpPost("fill-blank")]
        [Authorize]
        public async Task<ActionResult<FillBlankResponseDto>> GetFillBlank(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetFillBlank {request}");

            return await GetCurrentUserAsync(async userId => 
                await _activityService.GetFillBlank(request, userId, ct));
        }
    }
}
