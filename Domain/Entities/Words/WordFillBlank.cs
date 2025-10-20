using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Words
{
    public class WordFillBlank
    {
        public long Id { get; set; }

        [Required]
        public long WordId { get; set; }
        [ForeignKey(nameof(WordId))]
        public Word? Word { get; set; }

        [StringLength(256)]
        public string BlankTemplate { get; set; } = null!; // e.g., "The cat ___ on the mat."
        public int Difficulty { get; set; }

        public List<UserWordsProgress> WordProgresses { get; set; } = new();
    }
}
