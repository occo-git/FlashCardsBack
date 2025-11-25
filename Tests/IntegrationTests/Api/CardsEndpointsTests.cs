using Application.DTO.Users;
using Application.DTO.Words;
using Application.Mapping;
using Domain.Constants;
using Domain.Entities.Words;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.IntegrationTests.Api;

public class CardsEndpointsTests : BaseEndpointsTests
{
    public CardsEndpointsTests(IntegrationTestWebAppFactory factory) : base(factory)
    { }

    [Fact]
    public async Task GetCardById_ExistingCard_ReturnsCard()
    {
        // Arrange
        var word = await AddTestWordAsync();
        await AuthorizeAsync();

        // Act
        var response = await _client.GetAsync($"/api/cards/{word.Id}");

        // Assert
        await CheckResponseAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await response.Content.ReadFromJsonAsync<CardDto>();
        ValidateCardDto(card, word);
    }

    [Fact]
    public async Task GetCardById_NonExistingCard_ReturnsNotFound()
    {
        // Arrange
        var wordId = 999999; // does not exist
        await AuthorizeAsync();

        // Act
        var response = await _client.GetAsync($"/api/cards/{wordId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCardFromDeck_ValidRequest_ReturnsCardWithNeighbors()
    {
        // Arrange
        var word1 = await AddTestWordAsync(TestWordsA1[0]);
        var word2 = await AddTestWordAsync(TestWordsA1[1]);
        var word3 = await AddTestWordAsync(TestWordsA1[2]);
        await AuthorizeAsync();

        var request = new CardRequestDto(
            WordId: 0, // means the beginning of the deck
            Filter: new DeckFilterDto(Levels.A1)
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/cards/card-from-deck", request);

        // Assert
        await CheckResponseAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var card = await response.Content.ReadFromJsonAsync<CardExtendedDto>();
        card.Should().NotBeNull();
        card.Total.Should().Be(3);
        ValidateCardDto(card.Card, word1);
        card.Card!.WordText.Should().Be(word1.WordText);
    }

    [Fact]
    public async Task ChangeMark_ValidWordId_ChangesMarkAndReturnsOk()
    {
        // Arrange
        var word = await AddTestWordAsync();
        await AuthorizeAsync();

        var request = new WordRequestDto(word.Id);
        var requestCard = new CardRequestDto(
            WordId: word.Id,
            Filter: new DeckFilterDto(word.Level)
        );

        // Act
        var responseMark = await _client.PostAsJsonAsync("/api/cards/change-mark", request);
        var responseCardMarked = await _client.PostAsJsonAsync("/api/cards/card-from-deck", requestCard);
        var responseUnmark = await _client.PostAsJsonAsync("/api/cards/change-mark", request);
        var responseCardUnmarked = await _client.PostAsJsonAsync("/api/cards/card-from-deck", requestCard);

        // Assert
        await CheckResponseAsync(responseMark);
        await CheckResponseAsync(responseCardMarked);
        await CheckResponseAsync(responseUnmark);
        await CheckResponseAsync(responseCardUnmarked);
        responseMark.StatusCode.Should().Be(HttpStatusCode.OK);
        responseCardMarked.StatusCode.Should().Be(HttpStatusCode.OK);
        responseUnmark.StatusCode.Should().Be(HttpStatusCode.OK);
        responseCardUnmarked.StatusCode.Should().Be(HttpStatusCode.OK);

        var cardMarked = await responseCardMarked.Content.ReadFromJsonAsync<CardExtendedDto>();
        cardMarked.Should().NotBeNull();
        ValidateCardDto(cardMarked.Card, word, true); // true - card is marked
        var cardUnmarked = await responseCardUnmarked.Content.ReadFromJsonAsync<CardExtendedDto>();
        cardUnmarked.Should().NotBeNull();
        ValidateCardDto(cardUnmarked.Card, word, false); // false - card is unmarked
    }

    [Fact]
    public async Task GetWordList_ValidRequest_ReturnsStreamOfWords()
    {
        // Arrange
        var word1 = await AddTestWordAsync(TestWordsA1[0]);
        var word2 = await AddTestWordAsync(TestWordsA1[1]);
        await AuthorizeAsync();

        var request = new CardsPageRequestDto(
            WordId: 0, // means the beginning of the deck
            Filter: new DeckFilterDto(Levels.A1),
            isDirectionForward: true,
            PageSize: 10
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/cards/list", request);

        // Assert
        await CheckResponseAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var words = new List<WordDto?>();
        await foreach (var word in response.Content.ReadFromJsonAsAsyncEnumerable<WordDto?>())
            words.Add(word);

        words.Should().HaveCount(4); // 1 prev = null, 2 words, 1 next = null
        words[0].Should().BeNull(); // 1 prev = null
        words[1].Should().NotBeNull();
        words[1]!.WordText.Should().BeOneOf(word1.WordText, word2.WordText);
        words[2].Should().NotBeNull();
        words[2]!.WordText.Should().BeOneOf(word1.WordText, word2.WordText);
        words[3].Should().BeNull(); // 1 next = null
    }

    [Fact]
    public async Task GetLevels_ReturnsAllLevelsWithDescriptions()
    {
        // Arrange
        await AuthorizeAsync();

        // Act
        var response = await _client.GetAsync("/api/cards/levels");

        // Assert
        await CheckResponseAsync(response);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var levels = await response.Content.ReadFromJsonAsync<IEnumerable<LevelDto>>();

        levels.Should().NotBeNull();
        levels.Should().HaveCount(Levels.AllLevelsWithDescriptions.Count);
        levels.Should().Contain(l => l.Name == Levels.A1 && l.Description.Contains(Levels.AllLevelsWithDescriptions[Levels.A1]));
    }

    //[Fact]
    //public async Task GetThemes_ValidLevel_ReturnsThemes()
    //{
    //    // Предположим, что у тебя есть темы в БД или мок
    //    await AuthorizeAsync();

    //    var request = new LevelFilterDto { Level = Levels.A1 };

    //    var response = await _client.PostAsJsonAsync("/api/cards/themes", request);

    //    await CheckResponseAsync(response);
    //    response.StatusCode.Should().Be(HttpStatusCode.OK);

    //    var themes = new List<ThemeDto>();
    //    await foreach (var theme in response.Content.ReadFromJsonAsAsyncEnumerable<ThemeDto>())
    //    {
    //        themes.Add(theme!);
    //    }

    //    themes.Should().NotBeEmpty();
    //}

    [Fact]
    public async Task UnauthorizedAccess_ToAnyEndpoint_Returns401()
    {
        var endpoints = new List<(HttpMethods, string)> 
        {
            (HttpMethods.Get, "/api/cards/1"),
            (HttpMethods.Post, "/api/cards/card-from-deck"),
            (HttpMethods.Post, "/api/cards/change-mark"),
            (HttpMethods.Post, "/api/cards/list"),
            (HttpMethods.Get, "/api/cards/levels"),
            (HttpMethods.Post, "/api/cards/themes")
        };

        var emptyeRequest = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");        
        foreach (var ep in endpoints.Where(e => e.Item1 == HttpMethods.Get))
        {
            HttpResponseMessage response = await _client.GetAsync(ep.Item2);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        foreach (var ep in endpoints.Where(e => e.Item1 == HttpMethods.Post))
        {
            HttpResponseMessage response = await _client.PostAsync(ep.Item2, emptyeRequest);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    private void ValidateCardDto(CardDto? cardDto, Word word, bool isMarked = false)
    {
        cardDto.Should().NotBeNull();
        cardDto.Id.Should().Be(word.Id);
        cardDto.WordText.Should().Be(word.WordText);
        cardDto.PartOfSpeech.Should().Be(word.PartOfSpeech);
        cardDto.Transcription.Should().Be(word.Transcription);
        cardDto.Translation.Should().BeEquivalentTo(LocalizationMapper.GetDto(word.Translation));
        cardDto.Level.Should().Be(word.Level);
        cardDto.IsMarked.Should().Be(isMarked);
    }
}