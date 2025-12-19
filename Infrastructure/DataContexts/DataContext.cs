using Application.Abstractions.DataContexts;
using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Entities.Users;
using Domain.Entities.Words;
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
        public DbSet<User> Users { get; set; }

        public DbSet<Theme> Themes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<WordTheme> WordThemes { get; set; }
        public DbSet<WordFillBlank> FillBlanks { get; set; }
        public DbSet<UserBookmark> UserBookmarks { get; set; }
        public DbSet<UserWordsProgress> UserWordsProgress { get; set; }

        public DbSet<ResetPasswordToken> ResetPasswordTokens { get; set; }

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
                entity.HasIndex(u => u.UserName).IsUnique();
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
                .OnDelete(DeleteBehavior.Cascade); // delete User → delete Token
            #endregion

            #region ResetPasswordToken
            modelBuilder.Entity<ResetPasswordToken>()
                .HasOne(t => t.User)
                .WithMany() // no token collection in User
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // delete User → delete Token
            #endregion

            #region WordThemes
            // combined primary key
            modelBuilder.Entity<WordTheme>()
                .HasKey(wt => new { wt.WordId, wt.ThemeId });
            #endregion

            #region Words
            // rename field Word to WordText
            modelBuilder.Entity<Word>()
                .Property(w => w.WordText)
                .HasColumnName("Word");
            #endregion

            #region UserWordsProgress
            // relation with Word
            modelBuilder.Entity<UserWordsProgress>()
                .HasOne(wp => wp.Word)
                .WithMany(w => w.WordProgresses)
                .HasForeignKey(wp => wp.WordId)
                .IsRequired(false);

            // relation with FillBlank
            modelBuilder.Entity<UserWordsProgress>()
                .HasOne(wp => wp.FillBlank)
                .WithMany(fb => fb.WordProgresses)
                .HasForeignKey(wp => wp.FillBlankId)
                .IsRequired(false);
            #endregion

            #region UserBookmarks
            // ralation with Word
            modelBuilder.Entity<UserBookmark>()
                .HasOne(wp => wp.Word)
                .WithMany(w => w.Bookmarks)
                .HasForeignKey(wp => wp.WordId)
                .IsRequired(false);

            // ralation with User
            modelBuilder.Entity<UserBookmark>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(ub => ub.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade); // delete User → delete Bookmarks
            #endregion
        }
    }
}
