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
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken ct);
        //Task<User?> GetUserByUsernameAsync(string username, CancellationToken ct);
        //Task<User?> GetUserByEmailAsync(string email, CancellationToken ct);
        Task SetUserAsync(User user, CancellationToken ct);
        Task RemoveUserByIdAsync(Guid id, CancellationToken ct);
        Task RemoveUserByUsernameAsync(string username, CancellationToken ct);
    }

}
