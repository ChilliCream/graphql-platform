using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public interface ISortInputTypeDefinition
        : IHasScope
    {
        Type? EntityType { get; }
    }
}
