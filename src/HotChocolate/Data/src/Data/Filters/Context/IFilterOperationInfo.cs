namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents the value of a operation in filtering
/// </summary>
public interface IFilterOperationInfo
{
    /// <summary>
    /// The field this operation represents
    /// </summary>
    IFilterOperationField Field { get; }

    /// <summary>
    /// The value of this operation
    /// </summary>
    IFilterValueNode? Value { get; }
}
