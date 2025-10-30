using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record CardsPageRequestDto(long WordId, DeckFilterDto Filter, bool isDirectionForward, int PageSize);
}
