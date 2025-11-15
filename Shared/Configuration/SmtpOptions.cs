using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configuration
{
    public class SmtpOptions
    {
        public string Host {  get; set; } = String.Empty;
        public int Port { get; set; }
        public string Account { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string From { get; set; } = string.Empty;
    }
}