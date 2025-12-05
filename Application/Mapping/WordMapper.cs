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
        public static WordDto? ToWordDto(this CardDto? card)
        {
            if (card == null)
                return null;

            return new WordDto(
                card.Id,
                card.WordText,
                card.PartOfSpeech,
                card.Translation,
                card.IsMarked
            );
        }

        public static CardInfo ToCardInfo(this CardDto card)
        {
            return new CardInfo(card.Id, card.WordText);
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

        public static CardDto Mark(this CardDto card)
        {
            return card with { IsMarked = true };
        }
    }
}