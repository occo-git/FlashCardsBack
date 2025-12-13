using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Security
{
    public interface IUserPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyHashedPassword(string hash, string password);
    }
}
