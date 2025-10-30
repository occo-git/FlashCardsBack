using Application.DTO.Users;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validation
{
    public class CreateUserValidator : AbstractValidator<RegisterRequestDto>
    {
        //  TODO: correct email check, password check (special characters, etc.)
        public CreateUserValidator()
        {
            // Name not empty - 8...100 length
            RuleFor(user => user.Username)
                .NotEmpty().WithMessage("The user Name cannot be empty.")
                .Length(8, 100).WithMessage("The user Name should have a length of 8-100.");

            // Email not empty
            RuleFor(user => user.Email)
                .NotEmpty().WithMessage("The user Email cannot be empty.")
                .EmailAddress().WithMessage("The user Email format is not valid.");

            // Password not empty - 8...100 length
            RuleFor(user => user.Password)
                .NotEmpty().WithMessage("The user Password cannot be empty.")
                .Length(8, 100).WithMessage("The user Password should have a length of 8-100.");
        }
    }
}
