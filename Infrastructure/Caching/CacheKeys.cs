using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Caching
{
    public static class CacheKeys
    {
        public static string User(Guid id) => $"user:{id}";
        public static string UserBookmark(Guid userId) => $"user:bookmark:{userId}";
        public static string UserProgress(Guid userId, string activityType) => $"user:progress:{userId}:{activityType}";

        public static string RefreshToken(string token) => $"rt:{token}";
        public static string RefreshTokenValid(Guid userId, string sessionId) => $"rt:valid:{userId}:{sessionId}";

        public static string WordsByLevel(string level) => $"words:level:{level}";
        public static string WordsByTheme(long themeId) => $"words:theme:{themeId}";
        public static string WordsNeighbors(Guid userId, long wordId, string filterHash) =>  $"words:neighbors:{userId}:{wordId}:{filterHash}";

        public static string ThemesByLevel(string level) => $"themes:level:{level}";
    }
}