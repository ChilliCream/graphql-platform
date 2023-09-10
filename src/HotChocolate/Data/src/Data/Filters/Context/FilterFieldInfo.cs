namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents the value of a field in filtering
/// </summary>
public class FilterFieldInfo : IFilterFieldInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="FilterFieldInfo"/>
    /// </summary>
    public FilterFieldInfo(IFilterField field, IFilterValueNode value)
    {
        Field = field;
        Value = value;
    }

    /// <inheritdoc />
    public IFilterField Field { get; }

    /// <inheritdoc />
    public IFilterValueNode? Value { get; }
}
