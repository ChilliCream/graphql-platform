using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Represents a value of a filter.
/// </summary>
public class FilterValue : FilterValueNode, IFilterValue
{
    private object? _parsedObject;

    private readonly InputParser _inputParser;

    /// <summary>
    /// Creates a new instance of <see cref="FilterInfo"/>
    /// </summary>
    public FilterValue(IType type, IValueNode valueNode, InputParser inputParser)
        : base(type, valueNode)
    {
        _inputParser = inputParser;
    }

    /// <inheritdoc />
    public object? Value => _parsedObject ??= _inputParser.ParseLiteral(ValueNode, Type);
}
