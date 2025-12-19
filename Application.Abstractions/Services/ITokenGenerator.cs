using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface ITokenGenerator<T>
    {
        T GenerateToken(User user, string clientId, string? sessionId = null);
        int ExpiresInSeconds { get; }
    }
}