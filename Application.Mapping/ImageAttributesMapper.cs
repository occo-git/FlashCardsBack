using Application.DTO.Words;
using Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class ImageAttributesMapper
    {
        public static ImageAttributesDto GetDto(string? val)
        {
            if (string.IsNullOrEmpty(val)) return ImageAttributesDto.Empty;

            return JsonSerializer.Deserialize<ImageAttributesDto>(val) ?? ImageAttributesDto.Empty;
        }
    }
}
