namespace EquineBooking.Infrastructure.Options;

/// <summary>
/// Bound from the <c>SendGrid</c> configuration section / matching env vars.
/// </summary>
public sealed class SendGridOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "SendGrid";

    /// <summary>SendGrid API key with mail-send scope.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Sender email address (must be verified in SendGrid).</summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>Friendly sender name shown in the email "From" field.</summary>
    public string FromName { get; set; } = string.Empty;
}
