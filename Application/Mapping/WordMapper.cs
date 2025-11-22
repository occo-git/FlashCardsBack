using Application.DTO.Words;
using Application.UseCases;
using Domain.Constants;
using Domain.Entities;
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
        public static WordDto? ToWordDto(this (Word? Word, bool IsMarked) tuple)
        {
            if (tuple.Word == null) return null;
            return tuple.Word.ToWordDto(tuple.IsMarked);
        }
        public static WordDto ToWordDto(this Word word, bool isMarked = false)
        {
            return new WordDto(
                word.Id,
                word.WordText,
                word.PartOfSpeech,
                LocalizationMapper.GetDto(word.Translation),
                isMarked
            );
        }
        
        public static CardInfo ToCardInfo(this Word word)
        {
            return new CardInfo(word.Id, word.WordText);
        }

        public static CardDto? ToCardDto(this Word? word, bool isMarked = false)
        {
            if (word == null) return null;

            return new CardDto(
                word.Id,
                word.WordText,
                word.Transcription,
                word.PartOfSpeech,
                LocalizationMapper.GetDto(word.Translation),
                word.Example ?? String.Empty,
                word.Level,
                word.Difficulty,
                isMarked,
                ImageAttributesMapper.GetDto(word.ImageAttributes)
            );
        }
    }
}
