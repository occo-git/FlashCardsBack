using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.User
{
    public record RefreshTokenRequestDto
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
