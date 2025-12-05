using Application.DTO.Activity;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.DTO.Words;
using Domain.Constants;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using Shared;
using System.Net.Http.Json;
using Tests.LoadTests;


const int CONST_WordId = 1284;
var httpClient = Http.CreateDefaultClient();
var myCounter = Metric.CreateCounter("my-counter", unitOfMeasure: "MB");
var myGuage = Metric.CreateGauge("my-guage", unitOfMeasure: "KB");
var random = new Random();

#region Login tokens
var LoginTokens = await Login();
async Task<TokenResponseDto> Login()
{
    var response = await LoginStep();
    return response.Payload.Value;
}
#endregion

#region Steps
async Task<Response<TokenResponseDto>> LoginStep()
{
    var loginRequestDto = new LoginRequestDto("test_user", "123123123q");
    var sessionId = Guid.NewGuid();
    var request = Http.CreateRequest("POST", ApiRequests.AuthLogin)
        .WithHeader(HeaderNames.SessionId, sessionId.ToString())
        .WithJsonBody(loginRequestDto);

    var response = await Http.Send(httpClient, request);
    var tokens = await response.Payload.Value.Content.ReadFromJsonAsync<TokenResponseDto>();

    return tokens is null
        ? Response.Fail<TokenResponseDto>(message: "No JWT token available")
        : Response.Ok(payload: tokens);
}
async Task<Response<UserInfoDto>> MeStep(IScenarioContext context)
{
    var request = Get(ApiRequests.UsersMe);
    var response = await Http.Send(httpClient, request);
    var userInfo = await response.Payload.Value.Content.ReadFromJsonAsync<UserInfoDto>(context.ScenarioCancellationToken);

    return userInfo is null
        ? Response.Fail<UserInfoDto>(message: "User undefined")
        : Response.Ok(payload: userInfo);
}
async Task<Response<int>> LevelStep(IScenarioContext context, string level)
{
    LevelRequestDto requestDto = new LevelRequestDto(level);
    var request = Post(ApiRequests.UsersLevel, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<int>(context.ScenarioCancellationToken);

    return result > 0
        ? Response.Ok(payload: result)
        : Response.Fail<int>(message: "LevelStep incomplete");
}
async Task<Response<ProgressResponseDto>> ProgressStep(IScenarioContext context)
{
    var request = Get(ApiRequests.UsersProgress);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<ProgressResponseDto>(context.ScenarioCancellationToken);

    return result is null
        ? Response.Fail<ProgressResponseDto>(message: "ProgressStep incomplete")
        : Response.Ok(payload: result);
}
async Task<Response<int>> ProgressSaveStep(IScenarioContext context)
{
    ActivityProgressRequestDto requestDto = new ActivityProgressRequestDto(ActivityTypes.Quiz, WordId: CONST_WordId, null, true);
    var request = Post(ApiRequests.UsersProgressSave, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<int>(context.ScenarioCancellationToken);

    return result == 0
        ? Response.Fail<int>(message: "ProgressSaveStep incomplete")
        : Response.Ok(payload: result);
}
async Task<Response<CardExtendedDto>> CardFromDeckStep(IScenarioContext context)
{
    DeckFilterDto filter = new DeckFilterDto(Levels.A1);
    CardsPageRequestDto requestDto = new CardsPageRequestDto(WordId: CONST_WordId, Filter: filter, isDirectionForward: true, PageSize: 10);
    var request = Post(ApiRequests.CardsCardFromDeck, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<CardExtendedDto>(context.ScenarioCancellationToken);

    return result is null
        ? Response.Fail<CardExtendedDto>(message: "CardFromDeckStep incomplete")
        : Response.Ok(payload: result);
}
async Task<Response<IAsyncEnumerable<WordDto>>> CardsListStep(IScenarioContext context)
{
    DeckFilterDto filter = new DeckFilterDto(Levels.A1);
    CardsPageRequestDto requestDto = new CardsPageRequestDto(WordId: CONST_WordId, Filter: filter, isDirectionForward: true, PageSize: 10);
    var request = Post(ApiRequests.CardsList, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<IAsyncEnumerable<WordDto>>(context.ScenarioCancellationToken);

    //int count = 0;
    //await foreach (var word in result)
    //    count++;

    return result is null
        ? Response.Fail<IAsyncEnumerable<WordDto>>(message: "CardsListStep incomplete")
        : Response.Ok(payload: result);
}
async Task<Response<object>> TestMetricsStep(IScenarioContext context)
{
    await Task.Delay(100);

    var val = random.Next(100);
    myCounter.Add(val);
    myGuage.Set(val);

    return Response.Ok();
}
#endregion

#region Scenarios
var loginScenario = Scenario.Create("user_login_scenario",
    async context =>
    {
        return await Step.Run("login_step", context, LoginStep);
        //ArgumentNullException.ThrowIfNull(step1.Payload);
        //var tokens = step1.Payload.Value;
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(1))
    .WithLoadSimulations(GetSimulationSet(10, 1, 30));

var rate = 400;
var meScenario = GetScenario("users_me_scenario", MeStep, GetSimulationSet(3000));
var progressScenario = GetScenario("users_progress_scenario", ProgressStep, GetSimulationSet(rate));
var progressSaveScenario = GetScenario("users_progress_save_scenario", ProgressSaveStep, GetSimulationSet(rate));
var cardFromDeckScenario = GetScenario("cards_card_from_deck_scenario", CardFromDeckStep, GetSimulationSet(rate));
var cardsListScenario = GetScenario("cards_list_scenario", CardsListStep, GetSimulationSet(rate));

var testMetricsScenario = GetScenario("test_metrics_scenario", TestMetricsStep, GetSimulationSet())
    .WithInit(ctx =>
    {
        ctx.RegisterMetric(myCounter);
        ctx.RegisterMetric(myGuage);

        return Task.CompletedTask;
    })
    .WithThresholds(
        Threshold.Create(metric => metric.Counters.Get("my-counter").Value < 1000),
        Threshold.Create(metric => metric.Gauges.Get("my-gauge").Value >= 200)
    );
#endregion

#region Helpers
HttpRequestMessage Get(string url)
{
    return Http.CreateRequest("GET", url)
        .WithHeader("Authorization", $"Bearer {LoginTokens.AccessToken}")
        .WithHeader(HeaderNames.SessionId, LoginTokens.SessionId);
}
HttpRequestMessage Post<T>(string url, T requestDto)
{
    return Http.CreateRequest("POST", url)
        .WithHeader("Authorization", $"Bearer {LoginTokens.AccessToken}")
        .WithHeader(HeaderNames.SessionId, LoginTokens.SessionId)
        .WithJsonBody(requestDto);
}
ScenarioProps GetScenario<T>(string name, Func<IScenarioContext, Task<Response<T>>> run, LoadSimulation[] simulationSet)
{
    return Scenario.Create(name, async c => await run(c))
        .WithWarmUpDuration(TimeSpan.FromSeconds(1)) 
        .WithLoadSimulations(simulationSet);
}
LoadSimulation[] GetSimulationSet(int rate = 1000, int intervalSec = 5, int durationSec = 20) // (1000 requests per 5 sec = 200 RPS) x (20/5 = 4 times) = (1000 x 4 = 4000 requests)
{
    return new LoadSimulation[] {
        Simulation.RampingInject(rate: rate,
            interval: TimeSpan.FromSeconds(intervalSec),
            during: TimeSpan.FromSeconds(durationSec)),

        Simulation.Inject(rate: rate,
            interval: TimeSpan.FromSeconds(intervalSec),
            during: TimeSpan.FromSeconds(durationSec)),

        Simulation.RampingInject(rate: 0,
            interval: TimeSpan.FromSeconds(intervalSec),
            during: TimeSpan.FromSeconds(durationSec))
    };
}
#endregion

var stats = NBomberRunner
    //.RegisterScenarios(meScenario)//, progressScenario, progressSaveScenario, cardFromDeckScenario, cardsListScenario)
    .RegisterScenarios(cardFromDeckScenario, cardsListScenario)
    .WithReportFormats(ReportFormat.Html)
    .WithReportFileName("users_tests")
    .Run();

//var counterValue = stats.Metrics.Counters.Find("my-counter").Value;
//var gaugeValue = stats.Metrics.Gauges.Find("my-gauge").Value;