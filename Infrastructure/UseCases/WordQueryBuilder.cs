using Application.Abstractions.DataContexts;
using Application.DTO.Words;
using Application.UseCases;
using Domain.Entities.Words;
using Infrastructure.DataContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases
{
    public class WordQueryBuilder : IWordQueryBuilder
    {
        public IQueryable<Word> BuildQuery(IDataContext dbContext, DeckFilterDto filter)
        {
            var query = dbContext.Words.AsNoTracking().Where(w => w.Level == filter.Level);
            if (filter.IsMarked != 0)
                query = query.Where(w => (w.Mark ? 1 : -1) == filter.IsMarked);
            if (filter.ThemeId > 0)
                query = query.Where(w => w.WordThemes.Any(t => t.ThemeId == filter.ThemeId));
            if (filter.Difficulty > 0)
                query = query.Where(w => w.Difficulty == filter.Difficulty);
            return query;
        }
    }
}
