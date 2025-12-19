using Application.DTO.Email;
using Application.DTO.Users.EmailConfirmation;
using Application.DTO.Users.ResetPassword;
using Domain.Entities;

namespace Application.UseCases
{
    public interface IUserEmailService
    {
        Task SendGreeting(User user, CancellationToken ct);
        Task SendUsernameChanged(User user, string newUsername, CancellationToken ct);
        Task SendPasswordChanged(User user, CancellationToken ct);
        Task SendEmailConfirmationLink(SendLinkDto sendLinkDto, CancellationToken ct);
        Task SendResetPasswordLink(SendLinkDto sendLinkDto, CancellationToken ct);
    }
}