using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types.Collections;

public class SourceInterfaceTypeCollection
    : SourceMemberCollection<SourceInterfaceType>
    , ISourceComplexTypeCollection<SourceInterfaceType>
    , ISourceComplexTypeCollection<ISourceComplexType>
{
    public SourceInterfaceTypeCollection(IEnumerable<SourceInterfaceType> members)
        : base(members)
    {
    }

    ISourceComplexType ISourceMemberCollection<ISourceComplexType>.this[string schemaName]
        => this[schemaName];

    public ImmutableArray<SourceInterfaceType> Types
        => Members;

    ImmutableArray<ISourceComplexType> ISourceComplexTypeCollection<ISourceComplexType>.Types
        => [..Members];

    public bool TryGetType(string schemaName, [NotNullWhen(true)] out SourceInterfaceType? type)
        => TryGetMember(schemaName, out type);

    public bool TryGetType(string schemaName, [NotNullWhen(true)] out ISourceComplexType? type)
    {
        if(TryGetMember(schemaName, out var member))
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
