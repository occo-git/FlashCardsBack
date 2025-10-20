using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Words
{
    public record GetCardsDto(long LastId, int PageSize)
    {
        public static GetCardsDto Default => new(0, 10);
    }
}
