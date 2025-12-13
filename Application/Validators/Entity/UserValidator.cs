using Application.Exceptions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators.Entity
{
    public static class UserValidator
    {
        public static void ValidateActiveUser(User user)
        {
            if (!user.EmailConfirmed)
                throw new EmailNotConfirmedException("Account is not confirmed.");
            if (!user.Active)
                throw new AccountNotActiveException("Account is currently inactive. Please contact support.");
        }
    }
}
