using Application.DTO.Activity;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Domain.Constants;
using Microsoft.VisualBasic;
using NATS.Client.Internals;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Shared;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.LoadTests;

const int CONST_WordId = 1284;

var httpClient = Http.CreateDefaultClient();
var LoginTokens = await Login();
async Task<TokenResponseDto> Login()
{
    var response = await LoginStep();
    return response.Payload.Value;
}

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
async Task<Response<UserInfoDto>> MeStep()
{
    var request = Get(ApiRequests.UsersMe);
    var response = await Http.Send(httpClient, request);
    var userInfo = await response.Payload.Value.Content.ReadFromJsonAsync<UserInfoDto>();

    return userInfo is null
        ? Response.Fail<UserInfoDto>(message: "User undefined")
        : Response.Ok(payload: userInfo);
}
async Task<Response<int>> LevelStep(string level)
{
    LevelRequestDto requestDto = new LevelRequestDto(level);
    var request = Post(ApiRequests.UsersLevel, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<int>();

    return result > 0
        ? Response.Ok(payload: result)
        : Response.Fail<int>(message: "Set level incomplete");
}
async Task<Response<ProgressResponseDto>> ProgressStep()
{
    var request = Get(ApiRequests.UsersProgress);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<ProgressResponseDto>();

    return result is null
        ? Response.Fail<ProgressResponseDto>(message: "Get progress incomplete")
        : Response.Ok(payload: result);
}
async Task<Response<int>> ProgressSaveStep()
{
    ActivityProgressRequestDto requestDto = new ActivityProgressRequestDto(ActivityTypes.Quiz, WordId: CONST_WordId, null, true);
    var request = Post(ApiRequests.UsersProgressSave, requestDto);
    var response = await Http.Send(httpClient, request);
    var result = await response.Payload.Value.Content.ReadFromJsonAsync<int>();

    return result == 0
        ? Response.Fail<int>(message: "Set progress incomplete")
        : Response.Ok(payload: result);
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

var meScenario = GetScenario("users_me_scenario", MeStep(), GetSimulationSet());
var progressScenario = GetScenario("users_progress_scenario", ProgressStep(), GetSimulationSet());
var saveProgressScenario = GetScenario("users_progress_save_scenario", ProgressSaveStep(), GetSimulationSet());
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
ScenarioProps GetScenario<T>(string name, Task<Response<T>> step, LoadSimulation[] simulationSet)
{
    return Scenario.Create(name, async context => await step)
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
    .RegisterScenarios(saveProgressScenario)
    .WithReportFormats(ReportFormat.Html)
    .WithReportFileName("users_tests")
    .Run();