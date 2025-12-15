using Application.Abstractions.Services;
using Application.DTO.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Retry;
using Shared.Configuration;
using System.Net.Mail;
using System.Net.Sockets;

namespace Infrastructure.Services.EmailSender
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpOptions _smtpOptions;
        private readonly AsyncRetryPolicy _retryPolicy;
        private const int MaxRetryAttempts = 3;

        public EmailSender(IOptions<SmtpOptions> smtpOptions)
        {
            ArgumentNullException.ThrowIfNull(smtpOptions, nameof(smtpOptions));
            ArgumentNullException.ThrowIfNull(smtpOptions.Value, nameof(smtpOptions.Value));

            _smtpOptions = smtpOptions.Value;

            _retryPolicy = Policy
                .Handle<SmtpCommandException>() // MailKit specific exception
                .Or<SmtpProtocolException>()    // MailKit specific exception
                .Or<SocketException>()          // Network issues, example
                .Or<SmtpException>()         // General SMTP exception
                .WaitAndRetryAsync(MaxRetryAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public async Task SendEmailAsync(SendEmailDto emailDto, CancellationToken ct)
        {
            int retryAttempt = 0;
            await _retryPolicy.ExecuteAsync(async () =>
            {
                retryAttempt++;
                Console.WriteLine($"EmailSender: Try {retryAttempt}/{MaxRetryAttempts} to send email to {emailDto.ToEmail}");

                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(MailboxAddress.Parse(_smtpOptions.From));
                mimeMessage.To.Add(MailboxAddress.Parse(emailDto.ToEmail));
                mimeMessage.Subject = emailDto.Subject;
                mimeMessage.Body = new TextPart("html") { Text = emailDto.HtmlMessage };

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(_smtpOptions.Account, _smtpOptions.Password, ct);
                await client.SendAsync(mimeMessage, ct);
                await client.DisconnectAsync(true, ct);
            });
        }
    }
}