using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types;

public interface ISourceComplexTypeCollection<TType>
    : ISourceMemberCollection<TType>
    where TType : ISourceComplexType
{
    ImmutableArray<TType> Types { get; }
}
