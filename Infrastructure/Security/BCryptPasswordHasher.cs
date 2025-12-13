using Application.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Security
{
    public class BCryptPasswordHasher : IUserPasswordHasher
    {
        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, 12);

        public bool VerifyHashedPassword(string hash, string password)
            => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
