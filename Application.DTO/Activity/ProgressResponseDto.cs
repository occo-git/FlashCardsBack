using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Activity
{
    public record ProgressResponseDto(ProgressSummaryGroup[] Groups);

    public record ProgressSummaryGroup(string Name, string Key, int CorrectCount, int TotalAttempts);
}