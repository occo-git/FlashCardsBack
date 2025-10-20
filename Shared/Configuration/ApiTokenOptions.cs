using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class ApiTokenOptions
    {
        public int AccessTokenExpiresMinutes { get; set; } = 15; // default 15 minutes
        public int AccessTokenMinutesBeforeExpiration { get; set; } = 3; // default 3 minutes before expiration
        public int RefreshTokenExpiresDays { get; set; } = 7; // default 7 days
        public int RefreshTokenCleanupIntervalMinutes { get; set; } = 10; // default 10 minutes
    }
}
