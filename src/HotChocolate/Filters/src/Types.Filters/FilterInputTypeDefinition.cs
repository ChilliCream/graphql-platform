using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterInputTypeDefinition
        : InputObjectTypeDefinition
    {
        public Type? EntityType { get; set; }
    }
}
