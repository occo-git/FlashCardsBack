using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Users;
using Application.DTO.Users.EmailConfirmation;
using Application.DTO.Users.ResetPassword;
using Domain.Entities;

namespace Application.UseCases
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task<User?> GetByUsernameOrEmailAsync(string? text, CancellationToken ct);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken ct);
        IAsyncEnumerable<User?> GetAllAsyncEnumerable(CancellationToken ct);
        Task<User> CreateNewAsync(User user, CancellationToken ct);
        Task<User> CreateNewGoogleUserAsync(string googleEmail, CancellationToken ct);
        Task<User> AddAsync(User user, CancellationToken ct);
        Task<int> UpdateAsync(User user, CancellationToken ct);
        Task<int> SetLevel(Guid userId, string level, CancellationToken ct);
        Task<ProgressResponseDto> GetProgress(Guid userId, CancellationToken ct);
        Task<int> SaveProgress(Guid userId, ActivityProgressRequestDto request, CancellationToken ct);

        #region Email Confirmation
        Task<SendLinkDto> GenerateEmailConfirmationLinkAsync(User user, CancellationToken ct);
        Task<ConfirmEmailResponseDto> ConfirmEmailAsync(string token, CancellationToken ct);
        #endregion

        #region Reset Password
        Task<SendLinkDto> GenerateResetPasswordRequestLink(User user, CancellationToken ct);
        Task<bool> NewPasswordAsync(NewPasswordRequestDto request, CancellationToken ct);
        #endregion

        #region User Profile
        Task<User?> UpdateUsernameAsync(UpdateUsernameDto request, Guid userId, CancellationToken ct);
        Task<User?> UpdatePasswordAsync(UpdatePasswordDto request, Guid userId, CancellationToken ct);
        Task<int> DeleteProfileAsync(DeleteProfileDto request, Guid userId, CancellationToken ct);
        #endregion
    }
}