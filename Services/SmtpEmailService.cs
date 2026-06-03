using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamScheduler.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _configuration["Email:SmtpServer"];
            var portStr = _configuration["Email:Port"];
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderPassword = _configuration["Email:SenderPassword"];
            var enableSslStr = _configuration["Email:EnableSsl"];

            // Eğer ayarlar boşsa veya varsayılan değerse konsola loglayıp çıkalım (Mock/Test modu)
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword) || senderPassword.Contains("BURAYA"))
            {
                var output = $"\n==================================================\n" +
                             $"[MOCK EMAIL SENDER - TEST MODU]\n" +
                             $"Alıcı E-posta: {to}\n" +
                             $"Konu: {subject}\n" +
                             $"İçerik: {body}\n" +
                             $"==================================================\n";
                _logger.LogInformation(output);
                Console.WriteLine(output);
                return;
            }

            int port = string.IsNullOrEmpty(portStr) ? 587 : int.Parse(portStr);
            bool enableSsl = string.IsNullOrEmpty(enableSslStr) || bool.Parse(enableSslStr);

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(senderEmail, "Sınav Takip Sistemi");
                message.To.Add(new MailAddress(to));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(host, port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    client.EnableSsl = enableSsl;

                    _logger.LogInformation($"[EMAIL SENDER] E-posta gönderiliyor: {to}, Konu: {subject}");
                    await client.SendMailAsync(message);
                }
            }
        }
    }
}
