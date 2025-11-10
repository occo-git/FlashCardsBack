using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Words
{
    public class Word
    {
        public long Id { get; set; }
        [StringLength(30)]
        public string WordText { get; set; } = null!;
        [StringLength(20)]
        public string PartOfSpeech { get; set; } = null!; // e.g., noun, verb, adjective 
        [StringLength(30)]
        public string Transcription { get; set; } = null!;
        public string Translation { get; set; } = null!;
        [StringLength(256)]
        public string? Example { get; set; } = null;
        [StringLength(256)]
        public string? AudioUrl { get; set; }
        [StringLength(10)]
        public string Level { get; set; } = null!; // e.g., A1, A2, B1, B2, C1, C2
        public int Difficulty { get; set; }
        public bool Mark { get; set; }
        public string ImageAttributes { get; set; } = null!;


        public List<WordTheme> WordThemes { get; set; } = new();
        public List<WordFillBlank> FillBlanks { get; set; } = new();
        public List<UserWordsProgress> WordProgresses { get; set; } = new();
    }
}
