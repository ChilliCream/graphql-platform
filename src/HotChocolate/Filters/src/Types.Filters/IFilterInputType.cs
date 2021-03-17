using System;

namespace HotChocolate.Types.Filters
{
    /// <summary>
    /// Specifies a filter input type.
    /// </summary>
    [Obsolete("Use HotChocolate.Data.")]
    public interface IFilterInputType
        : INamedInputType
    {
        /// <summary>
        /// The entity on which the filter is applied.
        /// </summary>
        Type EntityType { get; }
    }
}
