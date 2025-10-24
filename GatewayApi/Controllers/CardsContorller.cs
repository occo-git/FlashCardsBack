using Application.DTO.Words;
using Application.Services.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace GatewayApi.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardsController : ControllerBase
    {
        private readonly IWordService _wordService;
        private readonly IUserService _userService;
        private readonly ILogger<CardsController> _logger;

        public CardsController(
            IWordService wordService,
            IUserService userService,
            ILogger<CardsController> logger)
        {
            _wordService = wordService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Get card by id
        /// GET: api/cards/{id}
        /// </summary>
        /// <param name="id">Card ID</param>
        /// <returns></returns>
        [HttpGet("{id:long}")]
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
        /// GET: api/cards/deck?lastId={lastId}&pageSize={pageSize}
        /// <param name="request">Request parameters</param>
        /// </summary>
        [HttpGet("deck")]
        public IAsyncEnumerable<CardDto?> GetCards(
            [FromQuery] long lastId,
            [FromQuery] int pageSize,
            CancellationToken ct)
        {
            return _wordService.GetCards(new GetCardsDto(lastId, pageSize), ct);
        }

    }
}
