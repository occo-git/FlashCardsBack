using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Extensions
{
    public static class ModelValidator
    {
        public static async Task<T> ValidationCheck<T>(this IValidator<T> validator, T item)
        {
            var validationResult = await validator.ValidateAsync(item);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                throw new ValidationException(string.Join("; ", errors));
            }
            return item;
        }
    }
}
