using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record WordDto(
        long Id, 
        string WordText,
        string PartOfSpeech,
        TranslationDto Translation,
        bool IsMarked
    );
}