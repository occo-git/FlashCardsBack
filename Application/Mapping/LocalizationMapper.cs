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
    public static class LocalizationMapper
    {
        public static TranslationDto GetDto(string? val)
        {
            if (string.IsNullOrEmpty(val)) return TranslationDto.Empty;

            return JsonSerializer.Deserialize<TranslationDto>(val) ?? TranslationDto.Empty;
        }

        public static string GetLocalization(string? val, string localization)
        {
            if (string.IsNullOrEmpty(val)) return String.Empty;

            var translation = JsonSerializer.Deserialize<TranslationDto>(val);
            return localization switch
            {
                Localization.Ru => translation?.ru ?? String.Empty,
                Localization.En => translation?.en ?? String.Empty,
                _ => String.Empty
            };
        }
    }
}
