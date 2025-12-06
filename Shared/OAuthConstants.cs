using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class OAuthConstants
    {
        public const string TokenTypeBearer = "Bearer";
        public const string ClientIdClaim = "client-id";

        public const string DefaultClientId = "gateway-default";
        public const string GrantTypePassword = "password";
        public const string GrantTypeRefreshToken = "refresh-token";

        public static readonly IReadOnlyDictionary<string, string[]> Clients = new Dictionary<string, string[]>
        { 
            [DefaultClientId] = new string[] { GrantTypePassword, GrantTypeRefreshToken }
        };
    }
}