using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IJwtTokenReader
    {           
        bool IsTokenExpired(string token);    
        Guid GetUserIdWithCheck(string token, string grantType);
    }
}