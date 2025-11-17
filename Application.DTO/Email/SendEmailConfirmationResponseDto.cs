using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Email
{
    public record SendEmailConfirmationResponseDto(bool Success, string Message);
}
