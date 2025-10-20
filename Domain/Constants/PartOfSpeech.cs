using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Constants
{
    public static class PartOfSpeech
    {
        public const string Noun = "noun";
        public const string Verb = "verb";
        public const string Adjective = "adjective";
        public const string PossessiveAdjective = "possessive adjective";
        public const string Adverb = "adverb";
        public const string Pronoun = "pronoun";
        public const string Preposition = "preposition";
        public const string Article = "article";
        public const string Conjunction = "conjunction";
        public const string Number = "number";
        public const string Quantifier = "quantifier";

        public static readonly HashSet<string> AllParts = new()
        {
            Noun, Verb, Adjective, PossessiveAdjective, Adverb, Pronoun, Preposition, Article, Conjunction, Number, Quantifier
        };

        public static bool IsValidPart(string part) => AllParts.Contains(part.ToLower());
    }
}
