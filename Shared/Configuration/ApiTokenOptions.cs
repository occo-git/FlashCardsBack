using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class ApiTokenOptions
    {
        public int ConfirmationTokenExpiresMinutes { get; set; } = 30; // 30 minutes
        public int ResetPasswordTokenExpiresMinutes { get; set; } = 15; // 15 minutes
        public int AccessTokenExpiresMinutes { get; set; } = 15; // 15 minutes
        public int AccessTokenBeforeExpirationMinutes { get; set; } = 3; // 3 minutes before expiration
        public int RefreshTokenExpiresDays { get; set; } = 7; // 7 days
        public int RefreshTokenCleanupIntervalMinutes { get; set; } = 10; // 10 minutes
    }
}
