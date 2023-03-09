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
        string? operationName,
        ObjectValueNode? variables,
        ObjectValueNode? extensions)
    {
        if (query is null && id is null && extensions is null)
        {
            throw new ArgumentException(OperationRequest_QueryOrPersistedQueryId, nameof(query));
        }

        Query = query;
        Id = id;
        OperationName = operationName;
        VariablesNode = variables;
        ExtensionsNode = extensions;
    }

    public OperationRequest(
        string? query = null,
        string? id = null,
        string? operationName = null,
        IReadOnlyDictionary<string, object?>? variables = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        if (query is null && id is null && extensions is null)
        {
            throw new ArgumentException(OperationRequest_QueryOrPersistedQueryId, nameof(query));
        }

        Query = query;
        Id = id;
        OperationName = operationName;
        Variables = variables;
        Extensions = extensions;
    }

    public string? Id { get; }

    public string? Query { get; }

    public string? OperationName { get; }

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
        => HashCode.Combine(Id, Query, Variables, Extensions);

    public static bool operator ==(OperationRequest left, OperationRequest right)
        => left.Equals(right);

    public static bool operator !=(OperationRequest left, OperationRequest right)
        => !left.Equals(right);
}
