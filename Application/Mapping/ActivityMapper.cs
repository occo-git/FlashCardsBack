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
    public static class ActivityMapper
    {
        public static FillBlankDto? ToFillBlankDto(this WordFillBlank? fillBlank)
        {
            if (fillBlank == null) return null;

            return new FillBlankDto(
                fillBlank.Id,
                fillBlank.WordId,
                fillBlank.BlankTemplate
            );
        }
    }
}
