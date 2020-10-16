using System;

namespace HotChocolate.Data.Filters
{
    public interface IFilterInputTypeDefinition
    {
        Type? EntityType { get; }

        string? Scope { get; }

        bool UseOr { get; }

        bool UseAnd { get; }
    }
}
