using Application.DTO.Activity;
using Domain.Entities;

namespace Application.UseCases
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
        Task<int> SetLevel(Guid userId, string level, CancellationToken ct);
        Task<ProgressResponseDto> GetProgress(Guid userId, CancellationToken ct);
        Task<int> SaveProgress(Guid userId, ActivityProgressRequestDto request, CancellationToken ct);
    }
}