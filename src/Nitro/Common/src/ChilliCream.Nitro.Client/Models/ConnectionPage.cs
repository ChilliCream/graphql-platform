namespace ChilliCream.Nitro.Client;

/// <summary>
/// Represents a forward-paginated result page.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed record ConnectionPage<T>(
    IReadOnlyList<T> Items,
    string? EndCursor,
    bool HasNextPage);
