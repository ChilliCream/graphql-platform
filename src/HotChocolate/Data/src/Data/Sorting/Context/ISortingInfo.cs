namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a collection of sorting fields and operations .
/// </summary>
public interface ISortingInfo
{
    /// <summary>
    /// Returns all sorting fields of this value
    /// </summary>
    IReadOnlyList<ISortingFieldInfo> GetFields();
}
