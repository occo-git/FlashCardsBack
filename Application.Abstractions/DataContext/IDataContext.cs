using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.DataContexts
{
    public interface IDataContext : IDisposable
    {
        DbSet<Theme> Themes { get; set; }
        DbSet<User> Users { get; set; }
        DbSet<RefreshToken> RefreshTokens { get; set; }
        DbSet<Word> Words { get; set; }
        DbSet<WordTheme> WordThemes { get; set; }
        DbSet<WordFillBlank> FillBlanks { get; set; }
        DbSet<UserBookmark> UserBookmarks { get; set; }
        DbSet<UserWordsProgress> UserWordsProgress { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
