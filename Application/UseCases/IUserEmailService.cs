using Application.DTO.Activity;
using Application.DTO.Email;
using Application.DTO.Tokens;
using Domain.Entities;

namespace Application.UseCases
{
    public interface IUserEmailService
    {
        Task<SendEmailConfirmationResponseDto> ReSendEmailConfirmation(string token, CancellationToken ct);
        Task<SendEmailConfirmationResponseDto> SendEmailConfirmation(User user, CancellationToken ct);
        Task<ConfirmEmailResponseDto> ConfirmEmailAsync(string token, CancellationToken ct);
    }
}