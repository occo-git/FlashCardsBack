using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record CardDto(
        long Id, 
        string WordText, 
        string Transcription,
        string PartOfSpeech,
        TranslationDto Translation, 
        string Example, 
        string Level, 
        bool isMarked,
        int Difficulty,
        ImageAttributesDto ImageAttributes);

    public record CardExtendedDto(CardDto? Card, CardInfo? PrevCard, CardInfo? NextCard, int Index, int Total);

    public record CardInfo(long Id, string WordText);
}
