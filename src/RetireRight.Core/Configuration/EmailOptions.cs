namespace RetireRight.Core.Configuration;

public class EmailOptions
{
    /// <summary>SMTP server hostname (e.g. smtp.gmail.com, smtp.resend.com, smtp.mailgun.org).</summary>
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@retireright.local";
    public string FromName { get; set; } = "RetireRight";
    /// <summary>STARTTLS (port 587) vs SSL on connect (port 465). Auto if true and port is 465.</summary>
    public bool UseSsl { get; set; } = false;
    /// <summary>If false, signup will still create the user but skip sending the verification email — useful for dev/local without SMTP.</summary>
    public bool Enabled { get; set; } = false;
}
