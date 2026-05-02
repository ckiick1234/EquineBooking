namespace EquineBooking.Infrastructure.Exceptions;

/// <summary>
/// Raised when a Cosmos DB write fails with HTTP 409 (id collision or pre-condition failure).
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
