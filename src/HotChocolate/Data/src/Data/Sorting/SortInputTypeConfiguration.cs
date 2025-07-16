using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public class SortInputTypeConfiguration
    : InputObjectTypeConfiguration
    , ISortInputTypeConfiguration
{
    public Type? EntityType { get; set; }

    public string? Scope { get; set; }

    internal bool IsNamed { get; set; }
}
