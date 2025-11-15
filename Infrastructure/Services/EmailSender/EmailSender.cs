using Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.EmailSender
{
    public class EmailSender : IEmailSender
    {
        private readonly SmtpClient _client;
        private readonly SmtpOptions _smtpOptions;

        public EmailSender(IOptions<SmtpOptions> smtpOptions)
        {
            ArgumentNullException.ThrowIfNull(smtpOptions, nameof(smtpOptions));
            ArgumentNullException.ThrowIfNull(smtpOptions.Value, nameof(smtpOptions.Value));

            _smtpOptions = smtpOptions.Value;
            _client = new SmtpClient(_smtpOptions.Host, _smtpOptions.Port)
            {
                Credentials = new NetworkCredential(_smtpOptions.Account, _smtpOptions.Password),
                EnableSsl = true
            };
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var mailMessage = new MailMessage(_smtpOptions.From, toEmail, subject, htmlMessage)
            {
                IsBodyHtml = true
            };
            await _client.SendMailAsync(mailMessage);
        }
    }
}