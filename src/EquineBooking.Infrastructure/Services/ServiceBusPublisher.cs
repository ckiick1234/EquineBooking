using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EquineBooking.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace EquineBooking.Infrastructure.Services;

/// <summary>
/// Publishes <see cref="BookingNotification"/> messages onto the
/// <c>booking-notifications</c> Service Bus queue.
/// </summary>
public sealed class ServiceBusPublisher : IAsyncDisposable
{
    public const string QueueName = "booking-notifications";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(ServiceBusClient client, ILogger<ServiceBusPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        _sender = client.CreateSender(QueueName);
        _logger = logger;
    }

    /// <summary>Serialize and enqueue a notification message.</summary>
    public async Task PublishAsync(BookingNotification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var json = JsonSerializer.Serialize(notification, SerializerOptions);
        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            Subject = notification.EventType.ToString(),
            MessageId = $"{notification.BookingId}:{notification.EventType}:{Guid.NewGuid():N}"
        };

        try
        {
            await _sender.SendMessageAsync(message, cancellationToken);
            _logger.LogInformation("Published {EventType} for booking {BookingId} to {Queue}",
                notification.EventType, notification.BookingId, QueueName);
        }
        catch (ServiceBusException ex)
        {
            _logger.LogError(ex, "Failed to publish {EventType} for booking {BookingId}",
                notification.EventType, notification.BookingId);
            throw;
        }
    }

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}
