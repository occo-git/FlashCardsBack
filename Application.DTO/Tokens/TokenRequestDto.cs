using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Tokens
{
    public record TokenRequestDto(
        string ClientId,
        string GrantType, 
        string? Username = null, 
        string? Password = null,
        string? RefreshToken = null,
        string? IdToken = null, // google
        string? ClientSecret = null,
        string? Scope = null);
}