using System;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// Specifies a filter input type.
    /// </summary>
    public interface IFilterInputType
        : INamedInputType
    {
        Type EntityType { get; }
    }
}
