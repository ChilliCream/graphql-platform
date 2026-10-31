namespace GreenDonut.Data.Internal;

/// <summary>
/// The direction in which a page is sliced from the dataset.
/// </summary>
internal enum PagingDirection
{
    /// <summary>
    /// The page is sliced from the start of the dataset using <c>first</c>.
    /// </summary>
    Forward,

    /// <summary>
    /// The page is sliced from the end of the dataset using <c>last</c>.
    /// </summary>
    Backward
}
