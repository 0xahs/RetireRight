using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RetireRight.Core.Configuration;
using RetireRight.Core.Interfaces;

namespace RetireRight.Api.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<RetireRightOptions> rrOptions, ILogger<SmtpEmailService> logger)
    {
        _opts = rrOptions.Value.Email;
        _logger = logger;
    }

    public bool Enabled => _opts.Enabled
        && !string.IsNullOrWhiteSpace(_opts.SmtpHost)
        && !string.IsNullOrWhiteSpace(_opts.FromAddress);

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!Enabled)
        {
            // No-op: caller logged the verification URL or otherwise handled the disabled case.
            _logger.LogWarning("Email send skipped (Email:Enabled=false or SmtpHost empty). To={To} Subject={Subject}",
                toAddress, subject);
            return;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opts.FromName, _opts.FromAddress));
        msg.To.Add(MailboxAddress.Parse(toAddress));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var smtp = new SmtpClient();
        // Port 465 → SslOnConnect; 587 → StartTls; everything else → auto-detect.
        var sslMode = _opts.SmtpPort switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _   => _opts.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto,
        };

        await smtp.ConnectAsync(_opts.SmtpHost, _opts.SmtpPort, sslMode, ct);
        if (!string.IsNullOrWhiteSpace(_opts.SmtpUsername))
        {
            await smtp.AuthenticateAsync(_opts.SmtpUsername, _opts.SmtpPassword, ct);
        }
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(quit: true, ct);

        _logger.LogInformation("Email sent to {To} subject={Subject}", toAddress, subject);
    }
}
