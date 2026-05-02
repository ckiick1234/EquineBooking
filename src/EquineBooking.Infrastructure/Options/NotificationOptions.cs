namespace EquineBooking.Infrastructure.Options;

/// <summary>
/// Notification template configuration sourced from <c>Admin__Email</c>,
/// <c>Facility__Name</c>, <c>Admin__BaseUrl</c>, and <c>Venmo__Url</c> env vars.
/// </summary>
public sealed class NotificationOptions
{
    /// <summary>Email address that receives admin-targeted notifications.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Display name for the facility, used in headers/footers.</summary>
    public string FacilityName { get; set; } = "Equine Facility";

    /// <summary>Base URL of the Angular admin app, used to build deep links.</summary>
    public string AdminBaseUrl { get; set; } = string.Empty;

    /// <summary>Public Venmo profile/payment URL embedded in approval emails.</summary>
    public string VenmoUrl { get; set; } = string.Empty;
}
