using System;
using System.Collections.Generic;
using HotChocolate.Language;
using static HotChocolate.Transport.Sockets.Client.Properties.SocketClientResources;

namespace HotChocolate.Transport.Sockets.Client;

public readonly struct OperationRequest : IEquatable<OperationRequest>
{
    public OperationRequest(
        string? query,
        string? id,
        ObjectValueNode? variables,
        ObjectValueNode? extensions)
    {
        if (query is null && id is null && extensions is null)
        {
            throw new ArgumentException(OperationRequest_QueryOrPersistedQueryId, nameof(query));
        }

        Query = query;
        Id = id;
        VariablesNode = variables;
        ExtensionsNode = extensions;
    }

    public OperationRequest(
        string? query = null,
        string? id = null,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        if (query is null && id is null && extensions is null)
        {
            throw new ArgumentException(OperationRequest_QueryOrPersistedQueryId, nameof(query));
        }

        Query = query;
        Id = id;
        Variables = variables;
        Extensions = extensions;
    }

    public string? Id { get; }

    public string? Query { get; }

    public IReadOnlyDictionary<string, object?>? Variables { get; }

    public ObjectValueNode? VariablesNode { get; }

    public IReadOnlyDictionary<string, object?>? Extensions { get; }

    public ObjectValueNode? ExtensionsNode { get; }

    public bool Equals(OperationRequest other)
        => Id == other.Id &&
            Query == other.Query &&
            Equals(Variables, other.Variables) &&
            Equals(Extensions, other.Extensions);

    public override bool Equals(object? obj)
        => obj is OperationRequest other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Id != null ? Id.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ (Query != null ? Query.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Variables != null ? Variables.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Extensions != null ? Extensions.GetHashCode() : 0);
            return hashCode;
        }
    }

    public static bool operator ==(OperationRequest left, OperationRequest right)
        => left.Equals(right);

    public static bool operator !=(OperationRequest left, OperationRequest right)
        => !left.Equals(right);
}
