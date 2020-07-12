using HotChocolate.Types;
using System;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// Specifies a filter input type.
    /// </summary>
    public interface IFilterInputType
        : INamedInputType
    {
        /// <summary>
        /// The entity on which the filter is applied.
        /// </summary>
        Type EntityType { get; }
    }
}
