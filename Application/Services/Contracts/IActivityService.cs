using Application.DTO.Activity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Contracts
{
    public interface IActivityService
    {
        Task<QuizResponseDto> GetQuiz(ActivityRequestDto request, CancellationToken ct);
    }
}
