using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Tokens
{
    public record AuthDto(TokenResponseDto Tokens, bool IsNewUser = false);
}
