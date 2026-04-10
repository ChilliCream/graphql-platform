using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Models;

public sealed class NodeIdValueSerializerInfo : SyntaxInfo
{
    public NodeIdValueSerializerInfo(
        INamedTypeSymbol compositeId,
        (string FilePath, int LineNumber, int CharacterNumber) location)
    {
        CompositeId = compositeId
            ?? throw new ArgumentNullException(nameof(compositeId));
        Location = location;
        OrderByKey = compositeId.ToFullyQualified();
    }

    public INamedTypeSymbol CompositeId { get; }

    public (string FilePath, int LineNumber, int CharacterNumber) Location { get; }

    public override string OrderByKey { get; }

    public override bool Equals(object? obj)
        => obj is NodeIdValueSerializerInfo other && Equals(other);

    public override bool Equals(SyntaxInfo? other)
    {
        if (other is not NodeIdValueSerializerInfo otherInfo)
        {
            return false;
        }

        return string.Equals(OrderByKey, otherInfo.OrderByKey, StringComparison.Ordinal)
            && Location.Equals(otherInfo.Location);
    }

    public override int GetHashCode()
        => HashCode.Combine(OrderByKey, Location);
}
