using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Activity
{
    public record ActivityProgressRequestDto(string ActivityType, long WordId, long? FillBlankId, bool IsSuccess);
}
