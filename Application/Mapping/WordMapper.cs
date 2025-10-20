using Application.DTO.Words;
using Domain.Constants;
using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class WordMapper
    {
        public static CardDto? ToDto(this Word? word)
        {
            if (word == null) return null;

            return new CardDto(
                word.Id, 
                word.WordText, 
                word.Transcription, 
                word.PartOfSpeech, 
                LocalizationMapper.GetLocalizationDto(word.Translation),
                word.Example ?? String.Empty,
                word.Level,
                word.Difficulty
            );
        }
    }
}
