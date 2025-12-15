using Application.DTO.Email;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IEmailQueue
    {
        Task QueueEmailAsync(SendEmailDto emailDto, CancellationToken ct);
        ChannelReader<SendEmailDto> Reader { get; }
    }
}
