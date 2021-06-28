using System;

namespace HotChocolate.Types.Sorting
{
    /// <summary>
    /// Specifies a sort input type.
    /// </summary>
    [Obsolete("Use HotChocolate.Data.")]
    public interface ISortInputType
        : INamedInputType
    {
        /// <summary>
        /// The entity on which the sorting is applied.
        /// </summary>
        Type EntityType { get; }
    }
}
