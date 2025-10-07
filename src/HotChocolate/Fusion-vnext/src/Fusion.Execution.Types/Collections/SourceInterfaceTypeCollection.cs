using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Types.Collections;

public class SourceInterfaceTypeCollection(IEnumerable<SourceInterfaceType> members)
    : SourceMemberCollection<SourceInterfaceType>(members)
    , ISourceComplexTypeCollection<SourceInterfaceType>
    , ISourceComplexTypeCollection<ISourceComplexType>
{
    ISourceComplexType ISourceMemberCollection<ISourceComplexType>.this[string schemaName]
        => this[schemaName];

    public ImmutableArray<SourceInterfaceType> Types
        => Members;

    ImmutableArray<ISourceComplexType> ISourceComplexTypeCollection<ISourceComplexType>.Types
        => [.. Members];

    public bool TryGetMember(string schemaName, [NotNullWhen(true)] out ISourceComplexType? type)
    {
        if (base.TryGetMember(schemaName, out var member))
        {
            type = member;
            return true;
        }

        type = null;
        return false;
    }

    IEnumerator<ISourceComplexType> IEnumerable<ISourceComplexType>.GetEnumerator()
        => GetEnumerator();
}
