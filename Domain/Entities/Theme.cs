using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Theme
    {
        public long Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Level { get; set; } = null!; // e.g., A1, A2, B1, B2, C1, C2

        public List<WordTheme> WordThemes { get; set; } = new();
    }
}
