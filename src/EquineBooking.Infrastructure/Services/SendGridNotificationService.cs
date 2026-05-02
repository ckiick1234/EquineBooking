using System.Net;
using System.Text;
using EquineBooking.Core.Interfaces;
using EquineBooking.Core.Models;
using EquineBooking.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EquineBooking.Infrastructure.Services;

/// <summary>
/// <see cref="INotificationService"/> backed by SendGrid. Templates are inlined HTML to
/// keep deployment simple — there is no SendGrid template ID to keep in sync.
/// </summary>
public sealed class SendGridNotificationService : INotificationService
{
    private const string Brand = "#2c5f2d";
    private const string Accent = "#97bc62";
    private const string Surface = "#f6f4ee";

    private readonly ISendGridClient _client;
    private readonly ISpaceRepository _spaces;
    private readonly SendGridOptions _sendGrid;
    private readonly NotificationOptions _notify;
    private readonly ILogger<SendGridNotificationService> _logger;

    public SendGridNotificationService(
        ISendGridClient client,
        ISpaceRepository spaces,
        IOptions<SendGridOptions> sendGrid,
        IOptions<NotificationOptions> notify,
        ILogger<SendGridNotificationService> logger)
    {
        _client = client;
        _spaces = spaces;
        _sendGrid = sendGrid.Value;
        _notify = notify.Value;
        _logger = logger;
    }

    public Task SendBookingCreatedAsync(Booking booking, CancellationToken cancellationToken = default)
        => SendCreatedAsync(booking, cancellationToken);

    public Task SendBookingApprovedAsync(Booking booking, CancellationToken cancellationToken = default)
        => SendApprovedAsync(booking, cancellationToken);

    public Task SendBookingDeclinedAsync(Booking booking, CancellationToken cancellationToken = default)
        => SendDeclinedAsync(booking, cancellationToken);

    public Task SendBookingCancelledAsync(Booking booking, CancellationToken cancellationToken = default)
        => SendCancelledAsync(booking, cancellationToken);

    public Task SendBookingModificationRequestedAsync(Booking booking, CancellationToken cancellationToken = default)
        => SendModificationRequestedAsync(booking, cancellationToken);

    private async Task SendCreatedAsync(Booking booking, CancellationToken ct)
    {
        var space = await _spaces.GetByIdAsync(booking.SpaceId, ct);
        var spaceName = space?.Name ?? booking.SpaceId;
        var approveUrl = BuildAdminUrl(booking.SpaceId, booking.Id, "approve");
        var declineUrl = BuildAdminUrl(booking.SpaceId, booking.Id, "decline");

        var body = new StringBuilder();
        body.Append($"<p>A new booking request needs review.</p>");
        body.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
        body.Append(Row("Space", spaceName));
        body.Append(Row("Requested", FormatRange(booking.StartTime, booking.EndTime)));
        body.Append(Row("Client", $"{Encode(booking.UserName)} &lt;{Encode(booking.UserEmail)}&gt;"));
        if (!string.IsNullOrWhiteSpace(booking.Notes))
        {
            body.Append(Row("Notes", Encode(booking.Notes)));
        }
        body.Append("</table>");
        body.Append("<p>")
            .Append($"<a href='{approveUrl}' style='background:{Brand};color:#fff;padding:10px 18px;text-decoration:none;border-radius:4px;margin-right:8px;'>Approve</a>")
            .Append($"<a href='{declineUrl}' style='background:#a33;color:#fff;padding:10px 18px;text-decoration:none;border-radius:4px;'>Decline</a>")
            .Append("</p>");

        await SendAsync(_notify.AdminEmail, "Admin", "New Booking Request", body.ToString(), ct);
    }

    private async Task SendApprovedAsync(Booking booking, CancellationToken ct)
    {
        var space = await _spaces.GetByIdAsync(booking.SpaceId, ct);
        var spaceName = space?.Name ?? booking.SpaceId;
        var rules = space?.Rules ?? string.Empty;

        var body = new StringBuilder();
        body.Append($"<p>Hi {Encode(booking.UserName)},</p>");
        body.Append("<p>Your booking has been confirmed. Details below:</p>");
        body.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
        body.Append(Row("Space", spaceName));
        body.Append(Row("Confirmed", FormatRange(booking.StartTime, booking.EndTime)));
        body.Append("</table>");
        if (!string.IsNullOrWhiteSpace(rules))
        {
            body.Append($"<h3 style='color:{Brand};margin-bottom:4px;'>Facility rules</h3>");
            body.Append($"<p style='white-space:pre-line;'>{Encode(rules)}</p>");
        }
        if (!string.IsNullOrWhiteSpace(_notify.VenmoUrl))
        {
            body.Append("<p>Please complete payment via Venmo:</p>")
                .Append($"<p><a href='{Encode(_notify.VenmoUrl)}' style='background:{Brand};color:#fff;padding:10px 18px;text-decoration:none;border-radius:4px;'>Pay on Venmo</a></p>");
        }

        await SendAsync(booking.UserEmail, booking.UserName, "Booking Confirmed", body.ToString(), ct);
    }

    private async Task SendDeclinedAsync(Booking booking, CancellationToken ct)
    {
        var space = await _spaces.GetByIdAsync(booking.SpaceId, ct);
        var spaceName = space?.Name ?? booking.SpaceId;

        var body = new StringBuilder();
        body.Append($"<p>Hi {Encode(booking.UserName)},</p>");
        body.Append("<p>Unfortunately, your booking request was not approved.</p>");
        body.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
        body.Append(Row("Space", spaceName));
        body.Append(Row("Requested", FormatRange(booking.StartTime, booking.EndTime)));
        body.Append("</table>");
        if (!string.IsNullOrWhiteSpace(booking.AdminNotes))
        {
            body.Append($"<h3 style='color:{Brand};margin-bottom:4px;'>Note from the facility</h3>");
            body.Append($"<p style='white-space:pre-line;'>{Encode(booking.AdminNotes)}</p>");
        }
        body.Append("<p>You're welcome to submit a new request for a different time.</p>");

        await SendAsync(booking.UserEmail, booking.UserName, "Booking Update", body.ToString(), ct);
    }

    private async Task SendCancelledAsync(Booking booking, CancellationToken ct)
    {
        var space = await _spaces.GetByIdAsync(booking.SpaceId, ct);
        var spaceName = space?.Name ?? booking.SpaceId;

        var body = new StringBuilder();
        body.Append("<p>A booking was cancelled.</p>");
        body.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
        body.Append(Row("Cancelled by", $"{Encode(booking.UserName)} &lt;{Encode(booking.UserEmail)}&gt;"));
        body.Append(Row("Space", spaceName));
        body.Append(Row("Original window", FormatRange(booking.StartTime, booking.EndTime)));
        body.Append("</table>");

        await SendAsync(_notify.AdminEmail, "Admin", "Booking Cancelled", body.ToString(), ct);
    }

    private async Task SendModificationRequestedAsync(Booking booking, CancellationToken ct)
    {
        var space = await _spaces.GetByIdAsync(booking.SpaceId, ct);
        var spaceName = space?.Name ?? booking.SpaceId;
        var reviewUrl = BuildAdminUrl(booking.SpaceId, booking.Id, "review");

        var body = new StringBuilder();
        body.Append($"<p>{Encode(booking.UserName)} has requested a change to an approved booking.</p>");
        body.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
        body.Append(Row("Space", spaceName));
        body.Append(Row("Requested window", FormatRange(booking.StartTime, booking.EndTime)));
        body.Append(Row("Client", $"{Encode(booking.UserName)} &lt;{Encode(booking.UserEmail)}&gt;"));
        if (!string.IsNullOrWhiteSpace(booking.Notes))
        {
            body.Append(Row("Client notes", Encode(booking.Notes)));
        }
        body.Append("</table>");
        body.Append($"<p><a href='{reviewUrl}' style='background:{Brand};color:#fff;padding:10px 18px;text-decoration:none;border-radius:4px;'>Review request</a></p>");

        await SendAsync(_notify.AdminEmail, "Admin", "Modification Request", body.ToString(), ct);
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string innerBody, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            _logger.LogWarning("Skipping {Subject} email — recipient address is empty", subject);
            return;
        }
        if (string.IsNullOrWhiteSpace(_sendGrid.ApiKey) || string.IsNullOrWhiteSpace(_sendGrid.FromEmail))
        {
            _logger.LogWarning("SendGrid not configured — would have sent {Subject} to {To}", subject, toEmail);
            return;
        }

        var html = WrapLayout(subject, innerBody);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(_sendGrid.FromEmail, _sendGrid.FromName),
            new EmailAddress(toEmail, toName),
            subject,
            plainTextContent: StripHtml(innerBody),
            htmlContent: html);

        var response = await _client.SendEmailAsync(message, ct);
        if (response.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
        {
            var errorBody = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError("SendGrid send failed for {Subject} to {To}: {Status} {Body}",
                subject, toEmail, response.StatusCode, errorBody);
        }
        else
        {
            _logger.LogInformation("Sent {Subject} to {To} ({Status})", subject, toEmail, response.StatusCode);
        }
    }

    private string WrapLayout(string title, string innerBody)
    {
        var facility = Encode(_notify.FacilityName);
        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>{Encode(title)}</title></head>
<body style='margin:0;padding:0;background:{Surface};font-family:Helvetica,Arial,sans-serif;color:#222;'>
  <table role='presentation' style='width:100%;background:{Surface};padding:24px 0;'>
    <tr><td align='center'>
      <table role='presentation' style='width:600px;max-width:90%;background:#fff;border-radius:6px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08);'>
        <tr><td style='background:{Brand};color:#fff;padding:18px 24px;font-size:18px;font-weight:bold;border-bottom:4px solid {Accent};'>{facility}</td></tr>
        <tr><td style='padding:24px;font-size:15px;line-height:1.5;'>{innerBody}</td></tr>
        <tr><td style='padding:16px 24px;background:{Surface};color:#666;font-size:12px;text-align:center;'>This is an automated message from {facility}.</td></tr>
      </table>
    </td></tr>
  </table>
</body></html>";
    }

    private string BuildAdminUrl(string spaceId, string bookingId, string action)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_notify.AdminBaseUrl) ? string.Empty : _notify.AdminBaseUrl.TrimEnd('/');
        return Encode($"{baseUrl}/admin/bookings/{spaceId}/{bookingId}?action={action}");
    }

    private static string Row(string label, string valueHtml) =>
        $"<tr><td style='padding:6px 0;color:#666;width:140px;'>{Encode(label)}</td><td style='padding:6px 0;'>{valueHtml}</td></tr>";

    private static string FormatRange(DateTimeOffset start, DateTimeOffset end) =>
        $"{start.ToLocalTime():ddd, MMM d yyyy h:mm tt} – {end.ToLocalTime():h:mm tt}";

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string StripHtml(string html)
    {
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var ch in html)
        {
            if (ch == '<') inTag = true;
            else if (ch == '>') inTag = false;
            else if (!inTag) sb.Append(ch);
        }
        return WebUtility.HtmlDecode(sb.ToString());
    }
}
