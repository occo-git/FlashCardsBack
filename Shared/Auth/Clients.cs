using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Auth
{
    public static class Clients
    {
        public const string ClientIdClaim = "client-id";

        public const string DefaultClientId = "gateway-default";
        public const string WebAppClientId = "web-app-client-id";

        public static readonly IReadOnlyDictionary<string, string[]> All = new Dictionary<string, string[]>
        {
            [DefaultClientId] = new string[] { GrantTypes.GrantTypeEmailConfirmation, GrantTypes.GrantTypePasswordReset },
            [WebAppClientId] = new string[] { GrantTypes.GrantTypePassword, GrantTypes.GrantTypeGoogle, GrantTypes.GrantTypeRefreshToken }
        };
    }
}