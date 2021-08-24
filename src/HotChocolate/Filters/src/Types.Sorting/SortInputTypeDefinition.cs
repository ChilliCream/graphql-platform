using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class SortInputTypeDefinition
        : InputObjectTypeDefinition
    {
        public Type? EntityType { get; set; }
    }
}
