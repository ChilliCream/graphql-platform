using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortInputTypeConfiguration : IHasScope
{
    Type? EntityType { get; }
}
