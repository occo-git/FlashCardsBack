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
        IAsyncEnumerable<CardDto?> GetCards(GetCardsDto? request, CancellationToken ct);
        Task<CardDto?> GetCardById(long wordId, CancellationToken ct);
    }
}
