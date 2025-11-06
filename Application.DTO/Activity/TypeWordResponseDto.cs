using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Activity
{
    public record TypeWordResponseDto(string ActivityType, WordDto Word);
}
