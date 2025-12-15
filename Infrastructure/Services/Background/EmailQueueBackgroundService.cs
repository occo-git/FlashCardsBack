using Application.Abstractions.Services;
using Application.DTO.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Infrastructure.Services.Background
{
    public class EmailQueueBackgroundService : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;

        public EmailQueueBackgroundService(
            IEmailQueue emailQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<EmailQueueBackgroundService> logger)
        {
            ArgumentNullException.ThrowIfNull(emailQueue, nameof(emailQueue));
            ArgumentNullException.ThrowIfNull(scopeFactory, nameof(scopeFactory));
            ArgumentNullException.ThrowIfNull(logger, nameof(logger));

            _emailQueue = emailQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var reader = _emailQueue.Reader;
            while (await reader.WaitToReadAsync(ct))
            {
                _logger.LogInformation("> EmailQueueBackgroundService.ExecuteAsync WaitToReadAsync");
                if (reader.TryRead(out var message))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    await emailSender.SendEmailAsync(message, ct);
                }
            }
        }
    }
}