namespace HotChocolate.Types.Pagination;

/// <summary>
/// This enum specified what parts of the connection where requested by the user.
/// </summary>
[Flags]
public enum ConnectionFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The edges field was requested by the user.
    /// </summary>
    Edges = 1,

    /// <summary>
    /// The nodes field was requested by the user.
    /// </summary>
    Nodes = 2,

    /// <summary>
    /// The total count field was requested by the user.
    /// </summary>
    TotalCount = 4,

    /// <summary>
    /// The page info field was requested by the user.
    /// </summary>
    PageInfo = 8,

    /// <summary>
    /// The relative cursor field was requested by the user.
    /// </summary>
    RelativeCursor = 16,

    /// <summary>
    /// All fields were requested by the user.
    /// </summary>
    All = Edges | Nodes | TotalCount | PageInfo | RelativeCursor
}
