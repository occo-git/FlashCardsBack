using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record DeckFilterDto(string Level, int IsMarked = 0, int ThemeId = 0, int Difficulty = 0);
}
