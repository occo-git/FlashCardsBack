using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IWordService
    {
        Task<CardDto?> GetCardById(long wordId, CancellationToken ct);
        IAsyncEnumerable<CardDto?> GetCards(CardsPageRequestDto request, CancellationToken ct);
        IAsyncEnumerable<WordDto?> GetWords(CardsPageRequestDto request, CancellationToken ct);        
        Task<CardDto?> ChangeMark(long wordId, CancellationToken ct);
        Task<CardExtendedDto?> GetCardWithNeighbors(CardRequestDto request, CancellationToken ct);
        IAsyncEnumerable<ThemeDto?> GetThemes(LevelFilterDto filter, CancellationToken ct);
    }
}
