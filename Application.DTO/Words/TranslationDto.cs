using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record TranslationDto(string en, string ru)
    {
        public static TranslationDto Empty => new TranslationDto(string.Empty, string.Empty);
    }
}