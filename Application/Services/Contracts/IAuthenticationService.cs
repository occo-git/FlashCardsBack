using Application.DTO.Tokens;
using Application.DTO.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IAuthenticationService
    {
        Task<TokenResponseDto> AuthenticateAsync(LoginRequestDto loginUserDto, string sessionId, CancellationToken ct);
        Task<TokenResponseDto> UpdateTokensAsync(string refreshToken, string sessionId, CancellationToken ct);
        Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct);
    }
}
