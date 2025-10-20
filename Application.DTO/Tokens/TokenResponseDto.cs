using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Tokens
{
    public record TokenResponseDto
    {
        public string AccessToken { get; init; } = string.Empty;
        public string RefreshToken { get; init; } = string.Empty;
        public string SessionId { get; init; } = string.Empty;

        public TokenResponseDto() { }

        public TokenResponseDto(string accessToken, string refreshToken, string sessionId) 
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            SessionId = sessionId;
        }

        public TokenResponseDto(TokenUpdatedResultDto dto)
        {
            AccessToken = dto.AccessToken;
            RefreshToken = dto.RefreshToken;
            SessionId = dto.SessionId;
        }
    }
}
