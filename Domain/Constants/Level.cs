using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Constants
{
    static class Level
    {
        public const string A1 = "A1";
        public const string A2 = "A2";
        public const string B1 = "B1";
        public const string B2 = "B2";
        public const string C1 = "C1";
        public const string C2 = "C2";

        public static readonly List<string> AllLevels = new() { A1, A2, B1, B2, C1, C2 };

        public static bool IsValidLevel(string level) => AllLevels.Contains(level);

        public static string GetNextLevel(string currentLevel)
        {
            int index = AllLevels.IndexOf(currentLevel);
            if (index == -1 || index == AllLevels.Count - 1)
                return currentLevel; // Return the same level if not found or already at highest level
            return AllLevels[index + 1];
        }

        public static string GetPreviousLevel(string currentLevel)
        {
            int index = AllLevels.IndexOf(currentLevel);
            if (index <= 0)
                return currentLevel; // Return the same level if not found or already at lowest level
            return AllLevels[index - 1];
        }

        public static int CompareLevels(string level1, string level2)
        {
            int index1 = AllLevels.IndexOf(level1);
            int index2 = AllLevels.IndexOf(level2);
            return index1.CompareTo(index2);
        }
    }
}
