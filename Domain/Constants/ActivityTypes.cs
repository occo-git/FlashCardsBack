using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Constants
{
    public static class ActivityTypes
    {
        public const string Quiz = "Quiz";
        public const string TypeWord = "Type Word";
        public const string ChooseBlank = "Choose Blank";
        public const string FillBlank = "Fill Blank";

        public static readonly HashSet<string> AllActivityTypes = new() { Quiz, TypeWord, ChooseBlank, FillBlank };
    }
}