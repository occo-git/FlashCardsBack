using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Tokens
{
    public record TokenResponseDto(
        string AccessToken, 
        string RefreshToken, 
        int ExpiresIn,
        string SessionId);
}
