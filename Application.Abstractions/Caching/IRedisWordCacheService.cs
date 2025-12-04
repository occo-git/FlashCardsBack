using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public interface IRedisWordCacheService
    {
        Task PreloadAllLevelsAsync(CancellationToken ct);

        Task<List<CardDto>> GetWordsByLevelAsync(string level, CancellationToken ct);
        Task<HashSet<long>> GetWordIdsByLevelAsync(string level, CancellationToken ct);

        Task<List<CardDto>> GetWordsByThemeAsync(long themeId, CancellationToken ct);
        Task<HashSet<long>> GetWordIdsByThemeAsync(long themeId, CancellationToken ct);

        Task<HashSet<long>> GetUserBookmarksAsync(Guid userId, CancellationToken ct);

        Task InvalidateBookmarksAsync(Guid userId, CancellationToken ct);
        Task InvalidateByLevelAsync(string level, CancellationToken ct);
    }
}