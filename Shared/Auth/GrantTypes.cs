using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Auth
{
    public static class GrantTypes
    {
        public const string GrantTypeEmailConfirmation = "email-confirmation";
        public const string GrantTypePasswordReset = "password-reset";
        public const string GrantTypePassword = "password";
        public const string GrantTypeGoogle = "google";
        public const string GrantTypeRefreshToken = "refresh-token";
    }
}