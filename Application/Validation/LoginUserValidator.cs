using Application.DTO.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validation
{
    public class LoginUserValidator : AbstractValidator<LoginUserDto>
    {
        public LoginUserValidator()
        {
            // Username not empty
            RuleFor(user => user.Username)
                .NotEmpty().WithMessage("The user Name cannot be empty.");

            // Password not empty
            RuleFor(user => user.Password)
                .NotEmpty().WithMessage("The user Password cannot be empty.");
        }
    }
}
