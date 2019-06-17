using System;

namespace HotChocolate.Client.Core
{
    /// <summary>
    /// Interface representing a PageInfo GraphQL entity.
    /// </summary>
    public interface IPageInfo : IQueryableValue<IPageInfo>
    {
        /// <summary>
        /// Gets the end cursor.
        /// </summary>
        string EndCursor { get; }

        /// <summary>
        /// Gets a value indicating whether more data remains.
        /// </summary>
        bool HasNextPage { get; }
    }
}
