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
        public string ConfirmEmailUrlTemplate { get; set; } = String.Empty;

        public int ReSendConfirmationTimeoutSeconds { get; set; } = 60;
        public int ReSendConfirmationAttemptsMax { get; set; } = 5;
    }
}
