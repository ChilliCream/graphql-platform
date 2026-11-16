namespace HotChocolate.Types.Pagination;

internal interface IStreamPageConnection
{
    /// <summary>
    /// Loads the data the given requirements describe, so the page answers them from memory.
    /// </summary>
    ValueTask LoadRequirementsAsync(
        StreamPageDataRequirements requirements,
        CancellationToken cancellationToken = default);

    ValueTask DisposeAsync();
}
