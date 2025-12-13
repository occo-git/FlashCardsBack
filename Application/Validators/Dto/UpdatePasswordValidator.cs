using Application.DTO.Users;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Dto
{
    public class UpdatePasswordValidator : AbstractValidator<UpdatePasswordDto>
    {
        public UpdatePasswordValidator()
        {
            // Password not empty - 8...100 length
            RuleFor(request => request.NewPassword)
                .NotEmpty().WithMessage("The user Password cannot be empty.")
                .Length(8, 100).WithMessage("The user Password should have a length of 8-100.");
        }
    }
}