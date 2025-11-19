using Application.Abstractions.Services;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Shared.Configuration;

namespace Infrastructure.Services.EmailSender
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpOptions _smtpOptions;

        public EmailSender(IOptions<SmtpOptions> smtpOptions)
        {
            ArgumentNullException.ThrowIfNull(smtpOptions, nameof(smtpOptions));
            ArgumentNullException.ThrowIfNull(smtpOptions.Value, nameof(smtpOptions.Value));

            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(_smtpOptions.From));
            mimeMessage.To.Add(MailboxAddress.Parse(toEmail));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("html") { Text = htmlMessage };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpOptions.Account, _smtpOptions.Password);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);
        }
    }
}