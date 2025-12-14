using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Email.Letters
{
    public record InformationLetterDto(string Title, string Caption, string[] Texts, string LoginLink);
}