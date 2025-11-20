using Application.DTO.Users;
using Application.Mapping;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests.Integration
{
    public abstract class BaseIntegrationTest<TService> : 
        IClassFixture<IntegrationTestWebAppFactory>, 
        IAsyncLifetime
        where TService : notnull
    {
        private readonly IServiceScope _scope; 
        protected readonly DataContext DbContext;
        protected readonly TService Service;

        protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
        {
            _scope = factory.Services.CreateScope();
            DbContext = _scope.ServiceProvider.GetRequiredService<DataContext>();
            Service = _scope.ServiceProvider.GetRequiredService<TService>();
        }

        public async Task InitializeAsync()
        {
            //Console.WriteLine("--> InitializeAsync");
            await ResetDatabaseAsync();
        }

        private async Task ResetDatabaseAsync()
        {
            Console.WriteLine("--> Resetting database");
            DbContext.UserBookmarks.RemoveRange(DbContext.UserBookmarks);
            DbContext.UserWordsProgress.RemoveRange(DbContext.UserWordsProgress);
            DbContext.WordThemes.RemoveRange(DbContext.WordThemes);
            DbContext.Themes.RemoveRange(DbContext.Themes);
            DbContext.Words.RemoveRange(DbContext.Words);
            DbContext.FillBlanks.RemoveRange(DbContext.FillBlanks);
            DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
            DbContext.Users.RemoveRange(DbContext.Users);
            await DbContext.SaveChangesAsync();
            //Console.WriteLine("<-- Resetting database");
        }

        public async Task InTransactionAsync(Func<DataContext, Task> testLogic)
        {
            Console.WriteLine("--> InTransactionAsync");
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                await testLogic(DbContext);
            }
            finally
            {
                await transaction.RollbackAsync();
                Console.WriteLine("<-- InTransactionAsync");
            }
        }

        #region Helpers
        protected async Task<User> CreateTestUserAsync(string username, string email, string password = "strongpassword123!!")
        {
            var user = GetUser(username, email, password);
            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
            return user;
        }

        protected User GetUser(string username, string email, string password = "strongpassword123!!")
        {
            var request = new RegisterRequestDto(username, email, password);
            return UserMapper.ToDomain(request);
        }

        protected async Task<Word> CreateTestWordAsync()
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

        public async Task DisposeAsync()
        {
            //Console.WriteLine("<-- DisposeAsync");
            if (DbContext != null) 
                await DbContext.DisposeAsync();
            _scope?.Dispose();
        }
    }
}