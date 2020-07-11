using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Sorting
{
    public class SortInputTypeDefinition
        : InputObjectTypeDefinition
    {
        public Type EntityType { get; set; }
    }
}
