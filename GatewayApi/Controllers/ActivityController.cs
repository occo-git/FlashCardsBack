using Application.DTO.Activity;
using Application.DTO.Words;
using Application.Services.Contracts;
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
            _logger.LogInformation($"GetQuiz {request}");
            if (request == null) 
                throw new ArgumentNullException(nameof(request));
            return await _activityService.GetQuiz(request, ct);
        }

        [HttpPost("type-word")]
        [Authorize]
        public async Task<TypeWordResponseDto> GetTypeWord(ActivityRequestDto request, CancellationToken ct)
        {
            _logger.LogInformation($"GetTypeWord {request}");
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return await _activityService.GetTypeWord(request, ct);
        }

        [HttpPost("fill-blank")]
        [Authorize]
        public async Task<FillBlankResponseDto> GetFillBlank(ActivityRequestDto request, CancellationToken ct)
        {
            _logger.LogInformation($"GetFillBlank {request}");
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return await _activityService.GetFillBlank(request, ct);
        }
    }
}
