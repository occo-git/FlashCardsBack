using Domain.Entities.Auth;
using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public record User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string UserName { get; set; }

        [Required]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [StringLength(10)]
        public string Level { get; set; } = null!; // e.g., A1, A2, B1, B2, C1, C2
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; }

        public string? SecureCode { get; set; }
        public DateTime? SecureCodeCreatedAt { get; set; }
        public int SecureCodeAttempts { get; set; }
        public bool EmailConfirmed { get; set; }
        
        public bool Active { get; set; }
        public string Provider { get; set; } = null!;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }


        public List<UserBookmark> Bookmarks { get; set; } = new();
        public List<UserWordsProgress> WordProgresses { get; set; } = new();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
