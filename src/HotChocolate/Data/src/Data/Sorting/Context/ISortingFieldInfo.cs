namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents the value of a field in sorting
/// </summary>
public interface ISortingFieldInfo
{
    /// <summary>
    /// The field this sorting operation represents
    /// </summary>
    ISortField Field { get; }

    /// <summary>
    /// The value of this field
    /// </summary>
    ISortingValueNode? Value { get; }
}
