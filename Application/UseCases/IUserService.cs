using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Domain.Entities;
using System.Runtime.CompilerServices;

namespace Application.UseCases
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
        IAsyncEnumerable<User?> GetAllAsyncEnumerable(CancellationToken ct);
        Task<User> CreateNewAsync(User user, CancellationToken ct);
        Task<User> AddAsync(User user, CancellationToken ct);
        Task<User> UpdateAsync(User user, CancellationToken ct);
        Task<int> SetLevel(Guid userId, string level, CancellationToken ct);
        Task<ProgressResponseDto> GetProgress(Guid userId, CancellationToken ct);
        Task<int> SaveProgress(Guid userId, ActivityProgressRequestDto request, CancellationToken ct);
    }
}