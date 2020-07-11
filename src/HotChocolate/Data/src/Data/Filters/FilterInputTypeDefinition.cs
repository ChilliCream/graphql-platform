using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDefinition
        : InputObjectTypeDefinition
    {
        public Type? EntityType { get; set; }
    }
}