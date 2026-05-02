using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;

namespace EquineBooking.Infrastructure.Data;

/// <summary>
/// Cosmos DB serializer that delegates to <see cref="System.Text.Json"/> so the domain
/// models' <c>[JsonPropertyName]</c> attributes are honoured (the default Cosmos serializer
/// uses Newtonsoft.Json and would ignore them).
/// </summary>
internal sealed class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _options;

    public CosmosSystemTextJsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default!;
            }

            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            return JsonSerializer.Deserialize<T>(stream, _options)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, _options);
        stream.Position = 0;
        return stream;
    }
}
