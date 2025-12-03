using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public interface IUserCacheService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        //Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
        //Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task SetAsync(User user, CancellationToken ct);
        Task RemoveByIdAsync(Guid id, CancellationToken ct);
    }
}