using Application.DTO.Activity;
using Application.DTO.Words;
using Application.UseCases;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/activity")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(
            IActivityService activityService,
            ILogger<ActivityController> logger)
        {
            _activityService = activityService;
            _logger = logger;
        }

        [HttpPost("quiz")]
        [Authorize]
        public async Task<QuizResponseDto> GetQuiz(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetQuiz {request}");
            return await _activityService.GetQuiz(request, ct);
        }

        [HttpPost("type-word")]
        [Authorize]
        public async Task<TypeWordResponseDto> GetTypeWord(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetTypeWord {request}");
            return await _activityService.GetTypeWord(request, ct);
        }

        [HttpPost("fill-blank")]
        [Authorize]
        public async Task<FillBlankResponseDto> GetFillBlank(ActivityRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetFillBlank {request}");
            return await _activityService.GetFillBlank(request, ct);
        }
    }
}
