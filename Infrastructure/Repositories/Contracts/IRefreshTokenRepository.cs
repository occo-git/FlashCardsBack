using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Contracts
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct);
        Task<RefreshToken?> GetRefreshTokenAsync(string tokenValue, CancellationToken ct);
        Task<int> RevokeRefreshTokensAsync(Guid userId, string sessionId, CancellationToken ct);
        Task<bool> ValidateRefreshTokenAsync(Guid userId, string sessionId, CancellationToken ct);
        Task<RefreshToken> UpdateRefreshTokenAsync(RefreshToken oldRefreshToken, RefreshToken newRefreshToken, CancellationToken ct);
    }
}
