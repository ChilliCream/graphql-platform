#nullable enable

namespace HotChocolate.Types.Pagination
{
    /// <summary>
    /// The paging result type.
    /// </summary>
    public interface IPageType : IObjectType
    {
        /// <summary>
        /// Gets the item type of the page.
        /// </summary>
        IOutputType ItemType { get; }
    }
}
