using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Words
{
    public class WordTheme
    {
        [Required]
        public long WordId { get; set; }
        [ForeignKey(nameof(WordId))]
        public Word? Word { get; set; }

        [Required]
        public long ThemeId { get; set; }
        [ForeignKey(nameof(ThemeId))]
        public Theme? Theme { get; set; }
    }
}
