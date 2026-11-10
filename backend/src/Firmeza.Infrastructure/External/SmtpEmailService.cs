using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace Firmeza.Infrastructure.External;

public sealed class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> log) : IEmailService
{
    public async Task<bool> SendReceiptAsync(string email, string number, FileDto receipt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(config["Smtp:Host"]) || string.IsNullOrWhiteSpace(config["Smtp:User"]))
        {
            log.LogWarning("SMTP no configurado. Comprobante {Number} pendiente de envío.", number);
            return false;
        }
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(config["Smtp:From"]) ? config["Smtp:User"]! : config["Smtp:From"]!));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = $"Firmeza · Comprobante {number}";
            var body = new BodyBuilder { TextBody = $"Gracias por tu compra. Adjuntamos el comprobante {number}." };
            body.Attachments.Add(receipt.Name, receipt.Content, ContentType.Parse(receipt.ContentType));
            message.Body = body.ToMessageBody();
            using var client = new SmtpClient();
            client.Timeout = 15000;
            await client.ConnectAsync(config["Smtp:Host"]!, config.GetValue("Smtp:Port", 587), SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(config["Smtp:User"]!, config["Smtp:Password"] ?? "", ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "No se pudo enviar el comprobante {Number}", number); return false; }
    }
}
