using Application.Abstractions.Caching;
using Application.Abstractions.DataContexts;
using Application.DTO.Words;
using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases
{
    public interface IWordQueryBuilder
    {
        IQueryable<Word> BuildDbQuery(IDataContext dbContext, DeckFilterDto filter, Guid userId);
        Task<IQueryable<Word>> BuildQueryCachedAsync(IDataContext dbContext, DeckFilterDto filter, Guid userId, CancellationToken ct);

        Task<IEnumerable<CardDto>> GetCardsListAsync(DeckFilterDto filter, Guid userId, CancellationToken ct);
        Task<IEnumerable<CardDto>> GetCardsActivityListAsync(DeckFilterDto filter, Guid userId, CancellationToken ct);
    }
}
