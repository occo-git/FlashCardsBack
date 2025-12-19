using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Repositories
{
    public interface IResetPasswordTokenRepository
    {
        Task<int> AddAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct);
        Task<int> UpdateAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct);
        Task<int> DeleteAsync(ResetPasswordToken resetPasswordToken, CancellationToken ct);
        Task<ResetPasswordToken?> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task<ResetPasswordToken?> GetAsync(string tokenValue, CancellationToken ct);
    }
}