using Application.DTO.Tokens;
using Application.DTO.Users;
using Microsoft.VisualBasic;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Shared;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

var httpClient = Http.CreateDefaultClient();

#region Login
var loginScenario = Scenario.Create(
    "user_login_scenario",
    async context =>
    {
        var step1 = await Step.Run("login_step", context, LoginStep);
        ArgumentNullException.ThrowIfNull(step1.Payload);
        var tokens = step1.Payload.Value;

        return step1;
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(5))
    .WithLoadSimulations(
        Simulation.Inject(
            rate: 5,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(10))
    );

async Task<Response<TokenResponseDto>> LoginStep()
{
    var loginRequestDto = new LoginRequestDto("test_user", "123123123q");
    var sessionId = Guid.NewGuid();
    var request = Http.CreateRequest("POST", "http://localhost:8080/api/auth/login")
        .WithJsonBody(loginRequestDto)
        .WithHeader(HeaderNames.SessionId, sessionId.ToString());

    var response = await Http.Send(httpClient, request);
    var tokens = await response.Payload.Value.Content.ReadFromJsonAsync<TokenResponseDto>();

    return tokens is null
        ? Response.Fail<TokenResponseDto>(message: "No JWT token available")
        : Response.Ok(payload: tokens);
}
#endregion

#region Me
var meScenario = Scenario.Create(
    "user_me_scenario",
    async context =>
    {
        var loginResponse = await Step.Run("login_step", context, LoginStep);
        var meResponse = await Step.Run("me_step", context, () => MeStep(loginResponse.Payload.Value));
        return meResponse;
    })
    .WithWarmUpDuration(TimeSpan.FromSeconds(5))
    .WithLoadSimulations(
        Simulation.Inject(
            rate: 5,
            interval: TimeSpan.FromSeconds(1),
            during: TimeSpan.FromSeconds(10))
    );

async Task<Response<UserInfoDto>> MeStep(TokenResponseDto tokens)
{
    var request = Http.CreateRequest("GET", "http://localhost:8080/api/users/me")
        .WithHeader("Authorization", $"Bearer {tokens.AccessToken}")
        .WithHeader(HeaderNames.SessionId, tokens.SessionId);

    var response = await Http.Send(httpClient, request);
    var userInfo = await response.Payload.Value.Content.ReadFromJsonAsync<UserInfoDto>();

    return userInfo is null
        ? Response.Fail<UserInfoDto>(message: "User undefined")
        : Response.Ok(payload: userInfo);
}
#endregion

NBomberRunner
    .RegisterScenarios(meScenario)
    .WithReportFormats(ReportFormat.Html)
    .WithReportFileName("users_tests")
    .Run();