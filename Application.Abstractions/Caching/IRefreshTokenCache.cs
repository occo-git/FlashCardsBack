using Domain.Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public interface IRefreshTokenCache
    {
        Task<RefreshToken?> GetAsync(string token, CancellationToken ct);
        Task SetAsync(RefreshToken token, CancellationToken ct);
        Task RemoveAsync(string token, CancellationToken ct);
    }
}