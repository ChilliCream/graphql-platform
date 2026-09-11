using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

public interface ISourceComplexTypeCollection<TType>
    : ISourceMemberCollection<TType>
    where TType : ISourceComplexType
{
    ImmutableArray<TType> Types { get; }
}
