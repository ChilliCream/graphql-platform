using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution;

/// <summary>
/// Represents a serializer for <see cref="IQueryResult"/>s.
/// </summary>
public interface IQueryResultSerializer
{
    /// <summary>
    /// Serializes a query result and writes it to the given <paramref name="stream"/>.
    /// </summary>
    /// <param name="result">
    /// The query result that shall be serialized.
    /// </param>
    /// <param name="stream">
    /// The stream to which the serialized <paramref name="result"/> shall be written to.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task SerializeAsync(
        IQueryResult result,
        Stream stream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializes a query result and writes it to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">
    /// The query result that shall be serialized.
    /// </param>
    /// <param name="writer">
    /// The writer to which the serialized <paramref name="result"/> shall be written to.
    /// </param>
    void Serialize(
        IQueryResult result,
        IBufferWriter<byte> writer);
}
