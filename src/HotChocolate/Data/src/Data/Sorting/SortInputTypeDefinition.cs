using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortInputTypeDefinition
        : InputObjectTypeDefinition
        , ISortInputTypeDefinition
    {
        public Type? EntityType { get; set; }

        public string? Scope { get; set; }
    }
}
