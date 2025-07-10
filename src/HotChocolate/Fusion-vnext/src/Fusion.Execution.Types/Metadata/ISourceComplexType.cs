using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

public interface ISourceComplexType : ISourceMember
{
    ImmutableArray<Lookup> Lookups { get; }
}
