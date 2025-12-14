using Application.DTO.Tokens;
using Application.DTO.Users;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IAuthenticationService
    {
        Task<User> RegisterAsync(RegisterRequestDto registerRequestDto, CancellationToken ct);
        Task<TokenResponseDto> AuthenticateAsync(TokenRequestDto loginUserDto, string sessionId, CancellationToken ct);
        Task<TokenResponseDto> AuthenticateGoogleUserAsync(string email, string clientId, string sessionId, CancellationToken ct);
        Task<TokenResponseDto> UpdateTokensAsync(TokenRequestDto request, string sessionId, CancellationToken ct);
        Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct);
    }
}