using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using Domain.Entities.Words;
using Infrastructure.DataContexts.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.DataContexts
{
    public class DataContext : DbContext, IDataContext
    {
        public DbSet<Theme> Themes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<WordTheme> WordThemes { get; set; }
        public DbSet<WordFillBlank> FillBlanks { get; set; }
        public DbSet<UserWordsProgress> UserWordsProgress { get; set; }


        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }      

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region User
            modelBuilder.Entity<User>(entity =>
            {
                // unique Username
                entity.HasIndex(u => u.Username).IsUnique();
                // unique Email
                entity.HasIndex(u => u.Email).IsUnique();
            });
            #endregion

            #region RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                // unique Token
                entity.HasIndex(rt => rt.Token).IsUnique();
            });
            // RefreshToken → User
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            // WordThemes: составной первичный ключ
            modelBuilder.Entity<WordTheme>()
                .HasKey(wt => new { wt.WordId, wt.ThemeId });

            // Words: переименование Word в WordText и CHECK для PartOfSpeech
            modelBuilder.Entity<Word>()
                .Property(w => w.WordText)
                .HasColumnName("Word");

            modelBuilder.Entity<Word>()
                .Property(w => w.PartOfSpeech)
                .HasColumnType("varchar(50)");

            // UserWordsProgress: связи с Word и FillBlank
            modelBuilder.Entity<UserWordsProgress>()
                .HasOne(wp => wp.Word)
                .WithMany(q => q.WordProgresses)
                .HasForeignKey(wp => wp.WordId)
                .IsRequired(false);

            modelBuilder.Entity<UserWordsProgress>()
                .HasOne(wp => wp.FillBlank)
                .WithMany(fb => fb.WordProgresses)
                .HasForeignKey(wp => wp.FillBlankId)
                .IsRequired(false);

            // UserWordsProgress: CHECK для ActivityType
            modelBuilder.Entity<UserWordsProgress>()
                .Property(wp => wp.ActivityType)
                .HasColumnType("varchar(20)");

            // Words: CHECK для Difficulty
            modelBuilder.Entity<Word>()
                .Property(w => w.Difficulty);

            // FillBlank: CHECK для Difficulty
            modelBuilder.Entity<WordFillBlank>()
                .Property(fb => fb.Difficulty);
        }
    }
}
