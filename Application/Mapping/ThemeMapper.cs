using Application.DTO.Words;
using Domain.Entities;
using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class ThemeMapper
    {
        public static ThemeDto? ToDto(this Theme? theme, int wordsCount = 0)
        {
            if (theme == null) return null;

            return new ThemeDto(
                theme.Id, 
                theme.Level, 
                LocalizationMapper.GetDto(theme.Name),
                wordsCount
            );
        }
    }
}
