namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents the value of a operation in filtering
/// </summary>
public class FilterOperationInfo : IFilterOperationInfo
{
    public FilterOperationInfo(
        IFilterOperationField field,
        IFilterValueNode value)
    {
        Field = field;
        Value = value;
    }

    /// <inheritdoc />
    public IFilterOperationField Field { get; }

    /// <inheritdoc />
    public IFilterValueNode? Value { get; }
}
