using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Represents a value of a sorting.
/// </summary>
public class SortingValue : SortingValueNode, ISortingValue
{
    private object? _parsedObject;

    private readonly InputParser _inputParser;

    /// <summary>
    /// Creates a new instance of <see cref="SortingInfo"/>
    /// </summary>
    public SortingValue(IType type, IValueNode valueNode, InputParser inputParser)
        : base(type, valueNode)
    {
        _inputParser = inputParser;
    }

    /// <inheritdoc />
    public object? Value => _parsedObject ??= _inputParser.ParseLiteral(ValueNode, Type);
}
