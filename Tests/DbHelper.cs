using Application.DTO.Tokens;
using Application.DTO.Users;
using Application.Mapping;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class DbHelper
    {
        public readonly DataContext DbContext;

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

        public DbHelper(DataContext dbContext)
        {
            DbContext = dbContext;
        }

        #region Database
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
        #endregion

        #region Users
        public async Task<User> AddConfirmedUserAsync(string username, string email, string password)
        {
            Console.WriteLine("--------------------------> AddConfirmedUserAsync");
            var user = GetUser(username, email, password);
            user.EmailConfirmed = true;
            user.Active = true;

            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User> AddTestUserAsync(string username, string email, string password = "strongpassword123!!")
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
        #endregion

        #region Words
        public async Task<Word> AddTestWordAsync(Word? word = null)
        {
            if (word == null)
                word = TestWord;

            DbContext.Words.Add(word);
            await DbContext.SaveChangesAsync();
            return word;
        }
        #endregion
    }
}
