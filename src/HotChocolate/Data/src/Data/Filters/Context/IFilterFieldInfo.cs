namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents the value of a field in filtering
/// </summary>
public interface IFilterFieldInfo
{
    /// <summary>
    /// The field this filter represents
    /// </summary>
    IFilterField Field { get; }

    /// <summary>
    /// The value of this field
    /// </summary>
    IFilterValueNode? Value { get; }
}
