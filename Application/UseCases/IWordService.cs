using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases
{
    public interface IWordService
    {
        Task<CardDto?> GetCardById(long wordId, CancellationToken ct);
        IAsyncEnumerable<ThemeDto?> GetThemes(LevelFilterDto filter, CancellationToken ct);
        Task<CardExtendedDto?> GetCardWithNeighbors(CardRequestDto request, Guid userId, CancellationToken ct);      
        Task<bool> ChangeMark(long wordId, Guid userId, CancellationToken ct);
        IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, Guid userId, CancellationToken ct);  
    }
}
