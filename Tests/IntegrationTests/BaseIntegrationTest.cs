using Application.DTO.Users;
using Application.Mapping;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace Tests.IntegrationTests
{
    public abstract class BaseIntegrationTest<TService> :
        IClassFixture<IntegrationTestWebAppFactory>,
        IAsyncLifetime
        where TService : notnull
    {
        private readonly IServiceScope _scope;
        protected readonly DataContext DbContext;
        protected readonly TService Service;

        #region Test Data
        private readonly Word TestWord = new Word
        {
            WordText = "red",
            PartOfSpeech = PartOfSpeech.Adjective,
            Transcription = "/rɛd/",
            Translation = "{\"en\": \"The color of blood or a tomato.\", \"ru\": \"красный\"}",
            Example = "The apple is red.",
            Level = Levels.A1,
            ImageAttributes = "{\"By\":\"MontyLov\",\"Link\":\"https://unsplash.com/photos/red-textile-HyBXy5PHQR8\",\"Source\":\"Unsplash\"}"
        };
        public readonly List<Word> TestWordsA1 = new()
        {
            new Word { WordText = "apple", PartOfSpeech = PartOfSpeech.Noun, Level = Levels.A1, Transcription = "/æpl/", Translation = "{\"en\": \"A round, juicy fruit that grows on trees, commonly red or green.\", \"ru\": \"яблоко\"}" },
            new Word { WordText = "eat", PartOfSpeech = PartOfSpeech.Verb, Level = Levels.A1, Transcription = "/iːt/", Translation = "{\"en\": \"To put food in your mouth and swallow it.\", \"ru\": \"есть\"}" },
            new Word { WordText = "run", PartOfSpeech = PartOfSpeech.Verb, Level = Levels.A1, Transcription = "/rʌn/", Translation = "{\"en\": \"To move quickly on foot.\", \"ru\": \"бежать\"}" },
            new Word { WordText = "and", PartOfSpeech = PartOfSpeech.Conjunction, Level = Levels.A1, Transcription = "/ænd/", Translation = "{\"en\": \"Used to connect words or sentences.\", \"ru\": \"и\"}" },
            new Word { WordText = "in", PartOfSpeech = PartOfSpeech.Preposition, Level = Levels.A1, Transcription = "/ɪn/", Translation = "{\"en\": \"Inside a place or time.\", \"ru\": \"в\"}" }
        };
        public readonly List<Word> TestWordsA2 = new()
        {
            new Word { WordText = "fantastic", PartOfSpeech = PartOfSpeech.Adjective, Level = Levels.A2, Transcription = "/fænˈtæstɪk/", Translation = "{\"en\": \"Very good or exciting.\", \"ru\": \"фантастический\"}" },
            new Word { WordText = "beautiful", PartOfSpeech = PartOfSpeech.Adjective, Level = Levels.A2, Transcription = "/ˈbjuːtɪf(ə)l/", Translation = "{\"en\": \"Very attractive or pleasing.\", \"ru\": \"красивый\"}" },
            new Word { WordText = "quickly", PartOfSpeech = PartOfSpeech.Adverb, Level = Levels.A2, Transcription = "/ˈkwɪklɪ/", Translation = "{\"en\": \"In a fast way.\", \"ru\": \"быстро\"}" }
        };
        #endregion
        
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
        protected async Task<User> AddConfirmedUserAsync(string username, string email, string password)
        {
            var request = new RegisterRequestDto(username, email, password);
            var user = UserMapper.ToDomain(request);
            user.EmailConfirmed = true;
            user.Active = true;

            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
            return user;
        }

        protected async Task<User> AddTestUserAsync(string username, string email, string password = "strongpassword123!!")
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

        protected async Task<Word> AddTestWordAsync(Word? word = null)
        {
            if (word == null)
                word = TestWord;

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