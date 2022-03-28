namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents the value of a field in filtering
/// </summary>
public class FilterFieldInfo : IFilterFieldInfo
{
    public FilterFieldInfo(IFilterField field, IFilterValueInfo value)
    {
        Field = field;
        Value = value;
    }

    /// <inheritdoc />
    public IFilterField Field { get; }

    /// <inheritdoc />
    public IFilterValueInfo? Value { get; }
}
