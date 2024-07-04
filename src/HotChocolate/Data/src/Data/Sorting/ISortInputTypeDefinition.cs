using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortInputTypeDefinition : IHasScope
{
    Type? EntityType { get; }
}
