using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Encapsulated all sorting specific information
/// </summary>
public class SortingContext : ISortingContext
{
    private readonly IReadOnlyList<SortingInfo> _value;
    private readonly IResolverContext _context;

    /// <summary>
    /// Creates a new instance of <see cref="SortingContext" />
    /// </summary>
    public SortingContext(
        IResolverContext context,
        IType type,
        IValueNode valueNode,
        InputParser inputParser)
    {
        _value = valueNode is ListValueNode listValueNode
            ? listValueNode.Items
                .Select(x => new SortingInfo(type, x, inputParser))
                .ToArray()
            : [new SortingInfo(type, valueNode, inputParser),];
        _context = context;
    }

    /// <inheritdoc />
    public void Handled(bool isHandled)
    {
        if (isHandled)
        {
            _context.LocalContextData = _context.LocalContextData.SetItem(SkipSortingKey, true);
        }
        else
        {
            _context.LocalContextData = _context.LocalContextData.Remove(SkipSortingKey);
        }
    }

    /// <inheritdoc />
    public bool IsDefined => _value is not [{ ValueNode.Kind: SyntaxKind.NullValue, },];

    /// <inheritdoc />
    public IReadOnlyList<IReadOnlyList<ISortingFieldInfo>> GetFields()
        => _value.Select(x => x.GetFields()).ToArray();

    /// <inheritdoc />
    public void OnAfterSortingApplied<T>(PostSortingAction<T> action)
        => _context.LocalContextData = _context.LocalContextData.SetItem(PostSortingActionKey, action);

    /// <inheritdoc />
    public IList<IDictionary<string, object?>> ToList()
        => _value
            .Select(Serialize)
            .OfType<IDictionary<string, object?>>()
            .Where(x => x.Count > 0)
            .ToArray();

    private static object? Serialize(ISortingValueNode? value)
    {
        switch (value)
        {
            case null:
                return null;

            case ISortingValueCollection collection:
                return collection.Select(Serialize).ToArray();

            case ISortingValue sortingValue:
                return sortingValue.Value;

            case ISortingInfo info:
                Dictionary<string, object?> data = new();

                foreach (var field in info.GetFields())
                {
                    SerializeAndAssign(field.Field.Name, field.Value);
                }

                return data;

                void SerializeAndAssign(string fieldName, ISortingValueNode? value)
                {
                    if (value is null)
                    {
                        data[fieldName] = null;
                    }
                    else
                    {
                        data[fieldName] = Serialize(value);
                    }
                }

            default:
                throw new InvalidOperationException();
        }
    }
}
