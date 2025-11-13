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
        IQueryable<Word> BuildQuery(IDataContext dbContext, DeckFilterDto filter, Guid userId);
    }
}
