using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class AuthOptions
    {
        public string GoogleClientId { get; set; } = String.Empty;
        public string GoogleClientSecret { get; set; } = String.Empty;
    }
}