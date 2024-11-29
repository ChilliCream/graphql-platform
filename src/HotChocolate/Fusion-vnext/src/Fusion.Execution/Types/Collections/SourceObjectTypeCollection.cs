using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types.Collections;

public class SourceObjectTypeCollection(IEnumerable<SourceObjectType> members)
    : SourceMemberCollection<SourceObjectType>(members)
    , ISourceComplexTypeCollection<SourceObjectType>
    , ISourceComplexTypeCollection<ISourceComplexType>
{
    ISourceComplexType ISourceMemberCollection<ISourceComplexType>.this[string schemaName]
        => this[schemaName];

    public ImmutableArray<SourceObjectType> Types
        => Members;

    ImmutableArray<ISourceComplexType> ISourceComplexTypeCollection<ISourceComplexType>.Types
        => [..Members];

    public bool TryGetType(string schemaName, [NotNullWhen(true)] out SourceObjectType? type)
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
