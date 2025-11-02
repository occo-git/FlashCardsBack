using Application.DTO.Words;
using Application.Services.Contracts;
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));
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
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return _wordService.GetWords(request, ct);
        }

        /// <summary>
        /// Get levels
        /// GET: api/cards/levels
        /// </summary>
        [HttpGet("levels")]
        [Authorize]
        public IEnumerable<string> GetLevels(CancellationToken ct)
        {
            return Levels.All;
        }

        /// <summary>
        /// Get levels
        /// POST: api/cards/themes
        /// </summary>
        [HttpPost("themes")]
        [Authorize]
        public IAsyncEnumerable<ThemeDto?> GetThemes(
            [FromBody] LevelFilterDto filter,
            CancellationToken ct)
        {
            _logger.LogInformation("CardsController.GetThemes: {filter}", filter);

            if (filter == null)
                throw new ArgumentNullException(nameof(filter));
            return _wordService.GetThemes(filter, ct);
        }
    }
}
