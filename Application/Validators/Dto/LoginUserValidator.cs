using Application.DTO.Tokens;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Dto
{
    public class LoginUserValidator : AbstractValidator<TokenRequestDto>
    {
        public LoginUserValidator()
        {
            // Username not empty
            RuleFor(request => request.Username)
                .NotEmpty().WithMessage("The user Name cannot be empty.")
                .Matches(@"^[a-zA-Z0-9._ % +-@]+$").WithMessage("Only Latin letters, digits, _ - . % + @ allowed"); 

            // Password not empty
            RuleFor(request => request.Password)
                .NotEmpty().WithMessage("The user Password cannot be empty.");
        }
    }
}