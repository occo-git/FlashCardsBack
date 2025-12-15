using Application.Abstractions.Services;
using Application.DTO.Email;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Infrastructure.Services.Background
{
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<SendEmailDto> _queue = Channel.CreateBounded<SendEmailDto>(1000);
        private readonly ILogger<EmailQueue> _logger;

        public EmailQueue(ILogger<EmailQueue> logger)
        {
            _logger = logger;
        }

        public async Task QueueEmailAsync(SendEmailDto emailDto, CancellationToken ct)
        {
            _logger.LogInformation("> EmailQueue.QueueEmailAsync");
            await _queue.Writer.WriteAsync(emailDto, ct);
        }

        public ChannelReader<SendEmailDto> Reader => _queue.Reader;
    }
}
