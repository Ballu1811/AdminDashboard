using System.Net;
using System.Net.Mail;
using ERP.WorkflowwServices.API.Interfaces;

namespace ERP.WorkflowwServices.API.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var host = configuration["Email:Smtp:Host"];
        var fromAddress = configuration["Email:Smtp:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            throw new EmailDeliveryException("Email delivery is not configured. Set Email:Smtp:Host and Email:Smtp:FromAddress.");

        if (!int.TryParse(configuration["Email:Smtp:Port"], out var port) || port is < 1 or > 65535)
            throw new EmailDeliveryException("Email:Smtp:Port must be a valid TCP port.");

        using var message = new MailMessage(fromAddress, recipient, subject, body);
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(configuration["Email:Smtp:EnableSsl"], out var enableSsl) && enableSsl
        };

        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException exception)
        {
            throw new EmailDeliveryException("The invitation email could not be delivered.", exception);
        }
    }
}
