using Application.DTO.Activity;
using Application.DTO.Words;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases
{
    public interface IActivityService
    {
        Task<QuizResponseDto> GetQuiz(ActivityRequestDto request, Guid userId, CancellationToken ct);
        Task<TypeWordResponseDto> GetTypeWord(ActivityRequestDto request, Guid userId, CancellationToken ct);
        Task<FillBlankResponseDto> GetFillBlank(ActivityRequestDto request, Guid userId, CancellationToken ct);
    }
}
