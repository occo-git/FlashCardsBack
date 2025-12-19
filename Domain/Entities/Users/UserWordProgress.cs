using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Users
{
    public class UserWordsProgress
    {
        public long Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public long WordId { get; set; }
        [ForeignKey(nameof(WordId))]
        public Word? Word { get; set; }

        [StringLength(20)]
        public string ActivityType { get; set; } = null!; // "Quiz" or "FillBlank"

        public long? FillBlankId { get; set; }
        [ForeignKey(nameof(FillBlankId))]
        public WordFillBlank? FillBlank { get; set; }

        public int CorrectCount { get; set; }
        public int TotalAttempts { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime NextReview { get; set; }
        public double SuccessRate { get; set; } // Calculated as CorrectCount / TotalAttempts
    }
}
