using Application.DTO.Activity;
using Application.DTO.Users;
using Application.DTO.Words;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardsController : UserControllerBase
    {
        private readonly IWordService _wordService;

        public CardsController(
            IWordService wordService,
            ILogger<CardsController> logger) : base(logger) 
        {
            _wordService = wordService;
        }

        /// <summary>
        /// Get card by id
        /// GET: api/cards/{id}
        /// </summary>
        /// <param name="id">Card ID</param>
        /// <returns></returns>
        [HttpGet("{id:long}")]
        [Authorize]
        public async Task<IActionResult> GetCardById([FromRoute] long id, CancellationToken ct)
        {
            var card = await _wordService.GetCardById(id, ct);
            if (card == null)
                return NotFound();

            return Ok(card);
        }

        /// <summary>
        /// Get cards
        /// POST: api/cards/card-from-deck
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("card-from-deck")]
        [Authorize]
        public async Task<ActionResult<CardExtendedDto?>> GetCardFromDeck([FromBody] CardRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetCardFromDeck {request}");

            var result = await GetCurrentUserAsync(async userId =>
                await _wordService.GetCardWithNeighbors(request, userId, ct));
            
            return Ok(result);
        }

        /// <summary>
        /// Changes card Mark property
        /// POST: api/cards/change-mark
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("change-mark")]
        [Authorize]
        public async Task<IActionResult> ChangeMark([FromBody] WordRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"ChangeMark {request}");

            await GetCurrentUser(async userId => 
                await _wordService.ChangeMark(request.WordId, userId, ct));

            return Ok();
        }

        /// <summary>
        /// Get cards
        /// POST: api/cards/list
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("list")]
        [Authorize]
        public IAsyncEnumerable<WordDto?> GetWordList([FromBody] CardsPageRequestDto request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            _logger.LogInformation($"GetWordList {request}");
            
            return GetCurrentUser(userId =>
                _wordService.GetWords(request, userId, ct));
        }

        /// <summary>
        /// Get Levels
        /// GET: api/cards/levels
        /// </summary>
        [HttpGet("levels")]
        [Authorize]
        public IEnumerable<LevelDto> GetLevels(CancellationToken ct)
        {
            return Levels.AllLevelsWithDescriptions.Select(d => new LevelDto(d.Key, d.Value));
        }

        /// <summary>
        /// Get Themes
        /// POST: api/cards/themes
        /// </summary>
        [HttpPost("themes")]
        [Authorize]
        public IAsyncEnumerable<ThemeDto?> GetThemes(
            [FromBody] LevelFilterDto filter,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(filter, nameof(filter));
            _logger.LogInformation("CardsController.GetThemes: {filter}", filter);

            return _wordService.GetThemes(filter, ct);
        }
    }
}
