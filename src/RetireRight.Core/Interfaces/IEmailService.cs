namespace RetireRight.Core.Interfaces;

public interface IEmailService
{
    /// <summary>Returns true when configured to actually send; false skips sending.</summary>
    bool Enabled { get; }
    Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken ct = default);
}
