using Application.DTO.Activity;
using Application.DTO.Users;
using Application.Exceptions;
using Application.Mapping;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Words;
using Infrastructure.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Integration
{
    public class UserEndpointTests : BaseIntegrationTest<IUserService>
    {
        public UserEndpointTests(IntegrationTestWebAppFactory factory)
            : base(factory)
        { }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = await CreateTestUserAsync("user1", "user1@test.com");

            // Act
            var result = await Service.GetByIdAsync(user.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.Id);
            Assert.Equal("user1", result.UserName);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await Service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUsernameAsync_ExistingUsername_ReturnsUser()
        {
            // Arrange
            await CreateTestUserAsync("uniqueuser", "email@test.com");

            // Act
            var result = await Service.GetByUsernameAsync("uniqueuser", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("uniqueuser", result!.UserName);
        }

        [Fact]
        public async Task GetByUsernameAsync_NonExistingUsername_ReturnsNull()
        {
            // Act
            var result = await Service.GetByUsernameAsync("nonexistent", CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
        {
            // Arrange
            await CreateTestUserAsync("user", "unique@test.com");

            // Act
            var result = await Service.GetByEmailAsync("unique@test.com", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("unique@test.com", result!.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
        {
            // Act
            var result = await Service.GetByEmailAsync("nope@nope.com", CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange
            await CreateTestUserAsync("user1", "u1@test.com");
            await CreateTestUserAsync("user2", "u2@test.com");
            await CreateTestUserAsync("user3", "u3@test.com");

            // Act
            var result = await Service.GetAllAsync(CancellationToken.None);

            // Assert
            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetAllAsyncEnumerable_ReturnsAsyncEnumerable()
        {
            // Arrange
            await CreateTestUserAsync("stream1", "s1@test.com");
            await CreateTestUserAsync("stream2", "s2@test.com");

            // Act
            var users = new List<User?>();
            var stream = Service.GetAllAsyncEnumerable(CancellationToken.None);
            await foreach (var user in stream)
                users.Add(user);

            // Assert
            Assert.Equal(2, users.Count);
        }

        [Fact]
        public async Task CreateNewAsync_DuplicateUsername_ThrowsUserAlreadyExistsException()
        {
            // Arrange
            await CreateTestUserAsync("duplicate", "diff1@test.com");
            var duplicateUser = GetUser("duplicate", "diff2@test.com");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UserAlreadyExistsException>(async () =>
                await Service.CreateNewAsync(duplicateUser, CancellationToken.None));

            Assert.Contains("username", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateNewAsync_DuplicateEmail_ThrowsUserAlreadyExistsException()
        {
            // Arrange
            await CreateTestUserAsync("userA", "same@test.com");
            var duplicateUser = GetUser("userB", "same@test.com");

            // Act & Assert
            var ex = await Assert.ThrowsAsync<UserAlreadyExistsException>(async () =>
                await Service.CreateNewAsync(duplicateUser, CancellationToken.None));

            Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUserProperties()
        {
            // Arrange
            var user = await CreateTestUserAsync("updatable", "up@test.com");

            user.UserName = "updatedname";
            user.Email = "updated@test.com";
            user.Level = "B2";

            // Act
            var updated = await Service.UpdateAsync(user, CancellationToken.None);

            // Assert
            var fromDb = await Service.GetByIdAsync(user.Id, CancellationToken.None);
            Assert.Equal("updatedname", fromDb!.UserName);
            Assert.Equal("updated@test.com", fromDb.Email);
            Assert.Equal("B2", fromDb.Level);
        }

        [Fact]
        public async Task SetLevel_ChangesUserLevel()
        {
            // Arrange
            var user = await CreateTestUserAsync("leveluser", "level@test.com");

            // Act
            var affected = await Service.SetLevel(user.Id, Levels.C1, CancellationToken.None);

            // Assert
            Assert.Equal(1, affected);
            var fromDb = await Service.GetByIdAsync(user.Id, CancellationToken.None);
            Assert.Equal("C1", fromDb!.Level);
        }

        [Fact]
        public async Task SetLevel_NonExistingUser_ThrowsKeyNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await Service.SetLevel(Guid.NewGuid(), "A1", CancellationToken.None));
        }

        [Fact]
        public async Task SaveProgress_CreatesNewProgressEntry()
        {
            // Arrange
            var word = CreateTestWordAsync();
            var user = await CreateTestUserAsync("progressuser", "p@test.com");
            var request = new ActivityProgressRequestDto(
                ActivityType: ActivityTypes.Quiz, 
                WordId: word.Id,
                FillBlankId: null,
                IsSuccess: true
            );

            // Act
            var affected = await Service.SaveProgress(user.Id, request, CancellationToken.None);

            // Assert
            Assert.Equal(1, affected);
            var progress = await DbContext.UserWordsProgress
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.WordId == word.Id);
            Assert.NotNull(progress);
            Assert.Equal(1, progress.CorrectCount);
            Assert.Equal(1, progress.TotalAttempts);
        }

        [Fact]
        public async Task SaveProgress_UpdatesExistingProgress()
        {
            // Arrange
            var word1 = CreateTestWordAsync();
            var user = await CreateTestUserAsync("progress2", "p2@test.com");
            var request1 = new ActivityProgressRequestDto(
                ActivityType: ActivityTypes.Quiz,
                WordId: word1.Id,
                FillBlankId: null,
                IsSuccess: true
            );
            await Service.SaveProgress(user.Id, request1, CancellationToken.None);

            // Act
            var request2 = new ActivityProgressRequestDto(
                ActivityType: ActivityTypes.Quiz,
                WordId: word1.Id,
                FillBlankId: null,
                IsSuccess: false
            );
            await Service.SaveProgress(user.Id, request2, CancellationToken.None);

            // Assert
            var progress = await DbContext.UserWordsProgress
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.WordId == word1.Id);
            Assert.NotNull(progress);
            Assert.Equal(1, progress.CorrectCount);
            Assert.Equal(2, progress.TotalAttempts);
            Assert.Equal(0.5, progress.SuccessRate);
        }

        [Fact]
        public async Task GetProgress_ReturnsCorrectSummary()
        {
            // Arrange
            var word1 = CreateTestWordAsync();
            var user = await CreateTestUserAsync("statsuser", "s@test.com");
            var request1 = new ActivityProgressRequestDto(
                ActivityType: ActivityTypes.Quiz,
                WordId: word1.Id,
                FillBlankId: null,
                IsSuccess: true
            );
            await Service.SaveProgress(user.Id, request1, CancellationToken.None);

            var word2 = CreateTestWordAsync();
            var request2 = new ActivityProgressRequestDto(
                ActivityType: ActivityTypes.Quiz,
                WordId: word2.Id,
                FillBlankId: null,
                IsSuccess: false
            );
            await Service.SaveProgress(user.Id, request2, CancellationToken.None);

            // Act
            var result = await Service.GetProgress(user.Id, CancellationToken.None);

            // Assert
            Assert.NotNull(result.Groups);
            var total = result.Groups.FirstOrDefault(g => g.Key == "Total");
            Assert.NotNull(total);
            Assert.Equal(1, total.CorrectCount);
            Assert.Equal(2, total.TotalAttempts);
        }

        #region Helpers
        private async Task<User> CreateTestUserAsync(string username, string email)
        {
            var user = GetUser(username, email);
            var created = await Service.CreateNewAsync(user, CancellationToken.None);
            return await Service.AddAsync(created, CancellationToken.None);
        }

        private User GetUser(string username, string email)
        {
            var request = new RegisterRequestDto
            {
                Username = username,
                Email = email,
                Password = "pass123"
            };
            return UserMapper.ToDomain(request);
        }

        private async Task<Word> CreateTestWordAsync()
        {
            var word = new Word
            {
                WordText = "wordtext",
                PartOfSpeech = PartOfSpeech.Noun,
                Transcription = "transcription",
                Translation = "translation",
                Level = Levels.A1
            };

            DbContext.Words.Add(word);
            await DbContext.SaveChangesAsync();
            return word;
        }
        #endregion
    }
}
