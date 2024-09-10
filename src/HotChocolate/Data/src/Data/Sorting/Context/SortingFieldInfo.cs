namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents the value of a field in sorting
/// </summary>
public class SortingFieldInfo : ISortingFieldInfo
{
    public SortingFieldInfo(ISortField field, ISortingValueNode value)
    {
        Field = field;
        Value = value;
    }

    /// <inheritdoc />
    public ISortField Field { get; }

    /// <inheritdoc />
    public ISortingValueNode? Value { get; }
}
