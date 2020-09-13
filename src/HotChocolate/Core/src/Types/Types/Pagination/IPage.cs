using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// Represents a page of a data set.
    /// </summary>
    public interface IPage
    {
        /// <summary>
        /// Gets the items of this page.
        /// </summary>
        IReadOnlyCollection<object> Items { get; }

        /// <summary>
        /// Gets basic information about this page in the overall data set.
        /// </summary>
        IPageInfo Info { get; }
    }
}
