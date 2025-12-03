using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Caching
{
    public static class CacheKeys
    {
        public static string RefreshTokenValid(Guid userId, string sessionId) =>
            $"rtv:{userId}:{sessionId}";

        public static string RefreshToken(string token) =>
            $"rt:{token}";

        public static string User(Guid id) =>
            $"user:{id}";

        public static string WordsByLevel(string level) =>
            $"words:level:{level}";

        public static string WordsAll() => 
            "words:all";

        public static string UserProgress(Guid userId, string activityType) =>
            $"user:progress:{userId}:{activityType}";

        public static string WordsNeighbors(Guid userId, long wordId, string filterHash) =>
            $"words:neighbors:{userId}:{wordId}:{filterHash}";
    }
}
