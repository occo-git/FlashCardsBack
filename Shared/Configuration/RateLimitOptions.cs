using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class RateLimitOptions
    {
        public bool Enabled {  get; set; }
        public int GeneralPermitLimit { get; set; } = 300; // per 1 minute
        public int AuthPermitLimit { get; set; } = 10; // per 1 minute
        public int UpdateUsernamePermitLimit { get; set; } = 5; // per 1 hour
        public int UpdatePasswordPermitLimit { get; set; } = 3; // per 1 hour
        public int DeleteProfilePermitLimit { get; set; } = 1; // per 1 day
    }
}