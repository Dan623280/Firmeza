using Firmeza.Application.Abstractions;
using Firmeza.Application.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
namespace Firmeza.Infrastructure.Email;

public sealed class SmtpEmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendReceiptAsync(string recipient, string saleNumber, byte[] pdf, CancellationToken ct)
    {
        var host = config["Smtp:Host"];
        var user = config["Smtp:UserName"];
        var password = config["Smtp:Password"];
        var from = config["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            throw new RequestException("smtp_not_configured", "Configure SMTP credentials.", 503);
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = $"Firmeza receipt {saleNumber}";
        var body = new BodyBuilder { TextBody = $"Thank you for your purchase. Attached is receipt {saleNumber}." };
        body.Attachments.Add($"{saleNumber}.pdf", pdf, new ContentType("application", "pdf"));
        message.Body = body.ToMessageBody();
        using var smtp = new SmtpClient { Timeout = 30000 };
        var port = config.GetValue("Smtp:Port", 587);
        await smtp.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(user, password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
