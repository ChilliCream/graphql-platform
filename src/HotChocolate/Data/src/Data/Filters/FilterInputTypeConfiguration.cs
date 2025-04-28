using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class FilterInputTypeConfiguration
    : InputObjectTypeConfiguration
    , IHasScope
    , IFilterInputTypeConfiguration
{
    public Type? EntityType { get; set; }

    public string? Scope { get; set; }

    public bool UseOr { get; set; }

    public bool UseAnd { get; set; }

    internal bool IsNamed { get; set; }
}
