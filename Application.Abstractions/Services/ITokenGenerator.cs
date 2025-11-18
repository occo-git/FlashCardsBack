using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface ITokenGenerator<T>
    {
        T GenerateToken(User user, string? sessionId = null);
        Guid GetUserId(string token);
        DateTime GetExpiration(string token);
        bool IsTokenExpired(string token);
    }
}
