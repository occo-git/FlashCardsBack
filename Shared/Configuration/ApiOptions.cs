using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class ApiOptions
    {
        public string OriginUrl { get; set; } = String.Empty;
        public string LoginUrl { get; set; } = String.Empty;
        public string ConfirmEmailUrl { get; set; } = String.Empty;
    }
}
