#if FUSION
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Transport;
#else
using System.Text.Json;

namespace HotChocolate.Transport;
#endif

/// <summary>
/// Represents a GraphQL request body that can be sent over a WebSocket connection or HTTP connection.
/// </summary>
public interface IRequestBody
{
    /// <summary>
    /// Writes a serialized version of this request to a <see cref="JsonWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The JSON writer.
    /// </param>
#if FUSION
    void WriteTo(JsonWriter writer);
#else
    void WriteTo(Utf8JsonWriter writer);
#endif
}
