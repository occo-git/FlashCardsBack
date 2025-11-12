using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class RedisOptions
    {
        public string? Host { get; set; }
        public int Timeout { get; set; }
        public string? InstanceName { get; set; }
    }
}
