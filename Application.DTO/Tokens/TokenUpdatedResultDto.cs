using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Tokens
{
    public record TokenUpdatedResultDto(bool IsUpdated, string AccessToken, string RefreshToken, string SessionId)
    {
        public bool IsNullOrEmpty => string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(RefreshToken);
    }
}
