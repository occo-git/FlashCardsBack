using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Auth
{
    public static class Providers
    {
        public const string ProviderLocal = "Local";
        public const string ProviderGoogle = "Google";

        public static readonly IReadOnlyCollection<string> All = new List<string>
        {
            ProviderLocal, ProviderGoogle
        };
    }
}