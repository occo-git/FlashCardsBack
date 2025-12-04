using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public interface ISmartWordCacheService
    {
        Task<List<CardDto>> GetWordsByLevelAsync(string level, CancellationToken ct);
        Task PreloadAllLevelsAsync(CancellationToken ct);
        Task<List<CardDto>> GetAllWordsAsync(CancellationToken ct);
        Task InvalidateLevelAsync(string level, CancellationToken ct);
    }
}