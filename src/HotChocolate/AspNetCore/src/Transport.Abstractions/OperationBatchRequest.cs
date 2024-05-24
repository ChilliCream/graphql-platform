using System;
using System.Collections.Generic;
using System.Text.Json;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport;

/// <summary>
/// Represents a GraphQL batch request that can be sent over a WebSocket or HTTP connection.
/// </summary>
/// <param name="requests">
/// A list of operation requests to execute.
/// </param>
public readonly struct OperationBatchRequest(
    IReadOnlyList<IOperationRequest> requests)
    : IRequestBody
    , IEquatable<OperationBatchRequest>
{
    /// <summary>
    /// Gets the list of operation requests to execute.
    /// </summary>
    public IReadOnlyList<IOperationRequest> Requests { get; } =
        requests ?? throw new ArgumentNullException(nameof(requests));

    /// <summary>
    /// Writes the request to the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">
    /// The writer to write the request to.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="writer"/> is <see langword="null"/>.
    /// </exception>
    public void WriteTo(Utf8JsonWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }
        
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
        if(Requests.Count != other.Requests.Count)
        {
            return false;
        }
        
        for (var i = 0; i < Requests.Count; i++)
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