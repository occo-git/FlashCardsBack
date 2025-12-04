using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class CacheServiceOptions
    {
        public int RefreshTokenTtlMinutes { get; set; } = 20;
        public int RefreshTokenValidationTtlMinutes { get; set; } = 10;
        public int UserTtlMinutes { get; set; } = 60;
        public int WordsTtlMinutes { get; set; } = 360;
        public int WordsSlideTimeMinutes { get; set; } = 30;
    }
}