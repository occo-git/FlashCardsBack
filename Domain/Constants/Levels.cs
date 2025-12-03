using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Constants
{
    public static class Levels
    {
        public const string A1 = "A1";
        public const string A2 = "A2";
        public const string B1 = "B1";
        public const string B2 = "B2";
        public const string C1 = "C1";
        public const string C2 = "C2";

        public static readonly IReadOnlyDictionary<string, string> AllLevelsWithDescriptions = new Dictionary<string, string>
        {
            [A1] = "Beginner", // Can use very basic phrases and expressions
            [A2] = "Elementary", // Can communicate in simple routine tasks
            [B1] = "Intermediate", // Can deal with most travel situations
            [B2] = "Upper-Intermediate", // Can interact with native speakers fluently
            [C1] = "Advanced", // Can use language flexibly and effectively
            [C2] = "Proficient", // Can express ideas spontaneously and precisely
        };

        public static string[] All
        {
            get { return new string[] { A1, A2, B1, B2, C1, C2 }; }
        }

        public static int Length => All.Length;
    }
}
