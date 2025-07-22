using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types;

public interface ISourceComplexTypeCollection<TType>
    : ISourceMemberCollection<TType>
    where TType : ISourceComplexType
{
    bool TryGetType(string schemaName, [NotNullWhen(true)] out TType? type);

    ImmutableArray<TType> Types { get; }
}
