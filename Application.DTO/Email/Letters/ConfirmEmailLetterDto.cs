using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Email.Letters
{
    public record ConfirmEmailLetterDto(string UserName, string ConfirmationLink);
}