using Application.DTO.Users;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Dto
{
    public class CreateUserValidator : AbstractValidator<RegisterRequestDto>
    {
        public CreateUserValidator()
        {
            // Username not empty - 8...100 length
            RuleFor(request => request.Username)
                .NotEmpty().WithMessage("The user Name cannot be empty.")
                .Length(8, 100).WithMessage("The user Name should have a length of 8-100.")
                .Matches(@"^[a-zA-Z0-9._ % +-@]+$").WithMessage("Only Latin letters, digits, _ - . % + @ allowed");

            // Email not empty
            RuleFor(request => request.Email)
                .NotEmpty().WithMessage("The user Email cannot be empty.")
                .EmailAddress().WithMessage("The user Email format is not valid.");

            // Password not empty - 8...100 length
            RuleFor(request => request.Password)
                .NotEmpty().WithMessage("The user Password cannot be empty.")
                .Length(8, 100).WithMessage("The user Password should have a length of 8-100.");
        }
    }
}