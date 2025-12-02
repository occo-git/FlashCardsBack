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
        public int GeneralPermitLimit { get; set; } = 100;
        public int AuthPermitLimit { get; set; } = 10;
    }
}