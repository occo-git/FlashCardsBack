using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface ITokenGenerator<T>
    {
        T GenerateToken(User user, string sessionId);
    }
}
