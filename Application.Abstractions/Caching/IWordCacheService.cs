using Application.DTO.Words;
using Domain.Entities.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public interface IWordCacheService
    {
        Task<CardDto?> GetWordAsync(long wordId);
        Task SetWordAsync(CardDto wordDto);

        //Task WarmupWordAsync(Word word);
        //Task WarmupSetPageAsync(long setId, int page);
        //Task InvalidateWordAsync(long wordId);
        //Task InvalidateSetAsync(long setId);
    }
}
