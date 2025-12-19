using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Users.ResetPassword
{
    public record NewPasswordRequestDto(string Token, string Email, string Password);
}