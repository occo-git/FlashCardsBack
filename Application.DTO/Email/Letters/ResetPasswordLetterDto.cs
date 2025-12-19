using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Email.Letters
{
    public record ResetPasswordLetterDto(string UserName, string ResetPasswordLink, int ExpiresInMinutes);
}