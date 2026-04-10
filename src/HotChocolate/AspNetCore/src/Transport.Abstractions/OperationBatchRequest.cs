#if FUSION
using System.Collections.Immutable;
using HotChocolate.Text.Json;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Transport.Serialization;

namespace HotChocolate.Fusion.Transport;
#else
using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport;
#endif

/// <summary>
/// Represents a GraphQL batch request that can be sent over a WebSocket or HTTP connection.
/// </summary>
public readonly struct OperationBatchRequest
    : IRequestBody
    , IEquatable<OperationBatchRequest>
{
#if FUSION
    /// <summary>
    /// Initializes a new instance of <see cref="OperationBatchRequest"/> with the specified
    /// immutable array of operation requests.
    /// </summary>
    /// <param name="requests">
    /// The requests of this batch.
    /// </param>
    /// <param name="fileMap">
    /// The file map entries for multipart file uploads. Default is empty.
    /// </param>
    public OperationBatchRequest(
        ImmutableArray<IOperationRequest> requests,
        ImmutableArray<FileEntry> fileMap = default)
    {
        if (requests.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "The batch request must contain at least one operation.",
                nameof(requests));
        }

        Requests = requests;
        FileMap = fileMap;
    }
#else
    /// <summary>
    /// Initializes a new instance of <see cref="OperationBatchRequest"/> with the specified
    /// immutable array of operation requests.
    /// </summary>
    /// <param name="requests">
    /// The requests of this batch.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="requests"/> is default or empty.
    /// </exception>
    public OperationBatchRequest(ImmutableArray<IOperationRequest> requests)
    {
        if (requests.IsDefaultOrEmpty)
        {
            throw new ArgumentException(
                "The batch request must contain at least one operation.",
                nameof(requests));
        }

        Requests = requests;
    }
#endif

    /// <summary>
    /// Gets the list of operation requests to execute.
    /// </summary>
    public ImmutableArray<IOperationRequest> Requests { get; }

#if FUSION
    /// <summary>
    /// Gets the file map entries for multipart file uploads.
    /// Each entry maps a file key in the variable JSON to the actual file stream,
    /// enabling the transport layer to construct the multipart form per the
    /// GraphQL multipart request specification.
    /// </summary>
    public ImmutableArray<FileEntry> FileMap { get; }
#endif

    /// <summary>
    /// Writes the request to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">
    /// The writer to write the request to.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
#if FUSION
    public void WriteTo(JsonWriter writer)
#else
    public void WriteTo(Utf8JsonWriter writer)
#endif
    {
        ArgumentNullException.ThrowIfNull(writer);

        Utf8JsonWriterHelper.WriteOperationRequest(writer, this);
    }

    /// <summary>
    /// Determines whether the specified <see cref="OperationBatchRequest"/>
    /// is equal to the current <see cref="OperationBatchRequest"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="OperationBatchRequest"/> to compare with the current <see cref="OperationBatchRequest"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the specified <see cref="OperationBatchRequest"/>
    /// is equal to the current <see cref="OperationBatchRequest"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(OperationBatchRequest other)
    {
        if (Requests.Length != other.Requests.Length)
        {
            return false;
        }

        for (var i = 0; i < Requests.Length; i++)
        {
            if (!Requests[i].Equals(other.Requests[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/>
    /// is equal to the current <see cref="OperationBatchRequest"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current <see cref="OperationBatchRequest"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the specified <see cref="object"/> is equal to the
    /// current <see cref="OperationBatchRequest"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object? obj)
        => obj is OperationBatchRequest other && Equals(other);

    /// <summary>
    /// Gets the hash code for the current <see cref="OperationBatchRequest"/>.
    /// </summary>
    /// <returns>
    /// The hash code for the current <see cref="OperationBatchRequest"/>.
    /// </returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var request in Requests)
        {
            hash.Add(request);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two specified <see cref="OperationBatchRequest"/> objects are equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationBatchRequest"/> to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationBatchRequest"/> to compare.
    /// </param>
    /// <returns></returns>
    public static bool operator ==(OperationBatchRequest left, OperationBatchRequest right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified <see cref="OperationBatchRequest"/> objects are not equal.
    /// </summary>
    /// <param name="left">
    /// The first <see cref="OperationBatchRequest"/> to compare.
    /// </param>
    /// <param name="right">
    /// The second <see cref="OperationBatchRequest"/> to compare.
    /// </param>
    /// <returns></returns>
    public static bool operator !=(OperationBatchRequest left, OperationBatchRequest right)
        => !left.Equals(right);
}
