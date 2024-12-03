namespace HotChocolate.Types.Pagination.Utilities;

/// <summary>
/// The paging flags express what parts of the connection the user requested.
/// </summary>
[Flags]
public enum PagingFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The edges or nodes were requested by the user.
    /// </summary>
    Edges = 1,

    /// <summary>
    /// The total count was requested by the user.
    /// </summary>
    TotalCount = 2
}
