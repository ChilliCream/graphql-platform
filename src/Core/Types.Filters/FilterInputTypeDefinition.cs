using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeDefinition
        : InputObjectTypeDefinition
    {
        public Type EntityType { get; set; }
    }
}
