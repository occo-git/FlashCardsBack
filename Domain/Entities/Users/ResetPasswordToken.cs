using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Users
{
    public class ResetPasswordToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int Attempts { get; set; } = 1;

        public ResetPasswordToken(Guid userId, string token)
        {
            Id = Guid.NewGuid();
            Token = token;
            UserId = userId;
        }
    }
}