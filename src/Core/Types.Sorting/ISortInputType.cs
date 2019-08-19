using System;

namespace HotChocolate.Types.Sorting
{
    public interface ISortInputType
        : INamedInputType
    {
        Type EntityType { get; }
    }
}
