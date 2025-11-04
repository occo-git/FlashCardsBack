using Application.DTO.Words;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IWordQueryBuilder
    {
        IQueryable<Word> BuildQuery(DataContext dbContext, DeckFilterDto filter);
    }
}
