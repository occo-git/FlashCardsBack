using Application.DTO.Users;
using Application.DTO.Words;
using Application.Services.Contracts;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardsController : ControllerBase
    {
        private readonly IWordService _wordService;
        private readonly ILogger<CardsController> _logger;

        public CardsController(
            IWordService wordService,
            ILogger<CardsController> logger)
        {
            _wordService = wordService;
            _logger = logger;
        }

        /// <summary>
        /// Get card by id
        /// GET: api/cards/{id}
        /// </summary>
        /// <param name="id">Card ID</param>
        /// <returns></returns>
        [HttpGet("{id:long}")]
        [Authorize]
        public async Task<IActionResult> GetCardById(
            [FromRoute] long id,
            CancellationToken ct)
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
        public Task<CardExtendedDto?> GetCardFromDeck(
            [FromBody] CardRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            return _wordService.GetCardWithNeighbors(request, ct);
        }

        /// <summary>
        /// Get cards
        /// POST: api/cards/deck
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("deck")]
        [Authorize]
        public IAsyncEnumerable<CardDto?> GetCardsFromDeck(
            [FromBody] CardsPageRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            return _wordService.GetCards(request, ct);
        }

        /// <summary>
        /// Changes card Mark property
        /// POST: api/cards/change-mark
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("change-mark")]
        [Authorize]
        public async Task<IActionResult> ChangeMark(
            [FromBody] WordRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            var card = await _wordService.ChangeMark(request.WordId, ct);
            if (card == null)
                return NotFound();

            return Ok(card);
        }

        /// <summary>
        /// Get cards
        /// POST: api/cards/list
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpPost("list")]
        [Authorize]
        public IAsyncEnumerable<WordDto?> GetWordList(
            [FromBody] CardsPageRequestDto request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            return _wordService.GetWords(request, ct);
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
