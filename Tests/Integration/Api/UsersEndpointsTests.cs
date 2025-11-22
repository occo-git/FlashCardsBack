using Application.DTO.Activity;
using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Mapping;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using FluentAssertions;
using Shared;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tests.Integration.Api;

public class UsersEndpointsTests : BaseEndpointsTests
{
    public UsersEndpointsTests(IntegrationTestWebAppFactory factory) : base(factory)
    { }

    [Fact]
    public async Task GetMe_AuthenticatedUser_ReturnsUserInfo()
    {
        // Arrange
        await AuthorizeAsync("meuser", "me@test.com");

        // Act
        var response = await _client.GetAsync("/api/users/me");
        await CheckResponseAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user.Should().NotBeNull();
        user!.Username.Should().Be("meuser");
    }

    [Fact]
    public async Task GetMe_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetLevel_ValidLevel_UpdatesAndReturnsAffectedRows()
    {
        var newLevel = Levels.B2;
        // Arrange
        await AuthorizeAsync();
        var request = new LevelRequestDto(Level: newLevel);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/level", request);
        await CheckResponseAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var affected = await response.Content.ReadFromJsonAsync<int>();
        affected.Should().Be(1);

        // Verify the level was updated
        var meResponse = await _client.GetAsync("/api/users/me");
        await CheckResponseAsync(meResponse);

        var user = await meResponse.Content.ReadFromJsonAsync<UserInfoDto>();
        user!.Level.Should().Be(newLevel);
    }

    [Fact]
    public async Task SaveProgress_ValidRequest_SavesAndReturnsAffectedRows()
    {
        // Arrange
        var word = await AddTestWordAsync();
        await AuthorizeAsync();
        var request = new ActivityProgressRequestDto(ActivityType: ActivityTypes.Quiz, WordId: word.Id, FillBlankId: null, IsSuccess: true);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/progress/save", request);
        await CheckResponseAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var affected = await response.Content.ReadFromJsonAsync<int>();
        affected.Should().Be(1);
    }

    [Fact]
    public async Task GetProgress_AfterSaveProgress_ReturnsCorrectStats()
    {
        // Arrange
        var word = await AddTestWordAsync();
        await AuthorizeAsync();
        var request = new ActivityProgressRequestDto(ActivityType: ActivityTypes.Quiz, WordId: word.Id, FillBlankId: null, IsSuccess: true);
        var saveRsponse = await _client.PostAsJsonAsync("/api/users/progress/save", request);
        await CheckResponseAsync(saveRsponse);

        // Act
        var response = await _client.GetAsync("/api/users/progress");
        await CheckResponseAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await response.Content.ReadFromJsonAsync<ProgressResponseDto>();
        progress.Should().NotBeNull();
        progress.Groups.Should().Contain(g => g.Key == "Total" && g.CorrectCount == 1);
    }

    [Fact]
    public async Task GetById_WithValidIdAndAuth_ReturnsUser()
    {
        // Arrange
        await AuthorizeAsync();       
        var meResponse = await _client.GetAsync("/api/users/me");
        await CheckResponseAsync(meResponse);
        var me = await meResponse.Content.ReadFromJsonAsync<UserInfoDto>();
        me.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/users/{me.Id}");
        await CheckResponseAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserInfoDto>();
        user!.Id.Should().Be(me.Id);
    }
}