using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tests.ContractTests.ProviderState
{
    public class ProviderStateRequest
    {
        [JsonPropertyName("state")]
        public string State { get; set; } = "test";

        [JsonPropertyName("params")]
        public Dictionary<string, string> Params { get; set; } = new();
    }
}
