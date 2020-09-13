using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        ValueTask<int> GetTotalCountAsync(CancellationToken cancellationToken);
    }
}
