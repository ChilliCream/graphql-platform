namespace HotChocolate.Data.Filters;

public interface IFilterInputTypeConfiguration
{
    Type? EntityType { get; }

    string? Scope { get; }

    bool UseOr { get; }

    bool UseAnd { get; }
}
