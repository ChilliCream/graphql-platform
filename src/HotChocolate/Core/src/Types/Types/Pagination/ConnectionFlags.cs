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
    Nothing = 0,

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
    /// The nodes or edges field was requested by the user.
    /// </summary>
    NodesOrEdges = Edges | Nodes,

    /// <summary>
    /// All fields were requested by the user.
    /// </summary>
    All = Edges | Nodes | TotalCount
}
