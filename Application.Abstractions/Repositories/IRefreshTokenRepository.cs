using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task<int> AddAsync(RefreshToken refreshToken, CancellationToken ct);
        Task<RefreshToken?> GetAsync(string tokenValue, CancellationToken ct);
        Task<int> RevokeAsync(Guid userId, string sessionId, CancellationToken ct);
        Task<bool> ValidateAsync(Guid userId, string sessionId, CancellationToken ct);
        Task<RefreshToken> UpdateAsync(RefreshToken oldRefreshToken, RefreshToken newRefreshToken, CancellationToken ct);
    }
}
