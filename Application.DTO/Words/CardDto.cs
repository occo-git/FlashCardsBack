using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record CardDto(
        long Id, 
        string Text, 
        string Transcription,
        string PartOfSpeech,
        TranslationDto Translation, 
        string Example, 
        string Level, 
        int Difficulty);
}
