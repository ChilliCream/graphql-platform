namespace HotChocolate.Types.Pagination;

/// <summary>
/// The paging result type.
/// </summary>
public interface IPageType : IObjectTypeDefinition
{
    /// <summary>
    /// Gets the item type of the page.
    /// </summary>
    IOutputType ItemType { get; }
}
