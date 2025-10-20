using Domain.Entities;

namespace Application.Services.Contracts
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
        Task<IAsyncEnumerable<User>> GetAllAsyncEnumerable(CancellationToken ct);
        Task<User> CreateAsync(User user, CancellationToken ct);
        Task<User> UpdateAsync(User user, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    }
}