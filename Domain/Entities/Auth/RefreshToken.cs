using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Auth
{
    public record RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string SessionId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Revoked { get; set; }

        public RefreshToken(string token, Guid userId, DateTime expiresAt, string sessionId)
        {
            Id = Guid.NewGuid();
            Token = token;
            UserId = userId;
            SessionId = sessionId;
            ExpiresAt = expiresAt;
            Revoked = false;
        }
    }
}
