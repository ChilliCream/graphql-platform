using System.Linq.Expressions;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Encapsulates all filter specific information
/// </summary>
public class FilterContext : IFilterContext
{
    private readonly FilterInfo _value;
    private readonly IResolverContext _context;

    /// <summary>
    /// Creates a new instance of <see cref="FilterContext" />
    /// </summary>
    public FilterContext(
        IResolverContext context,
        IType type,
        IValueNode valueNode,
        InputParser inputParser)
    {
        _value = new FilterInfo(type, valueNode, inputParser);
        _context = context;
    }

    /// <inheritdoc />
    public void Handled(bool isHandled)
    {
        if (isHandled)
        {
            _context.LocalContextData = _context.LocalContextData.SetItem(SkipFilteringKey, true);
        }
        else
        {
            _context.LocalContextData = _context.LocalContextData.Remove(SkipFilteringKey);
        }
    }

    /// <inheritdoc />
    public bool IsDefined => _value.ValueNode.Kind is not SyntaxKind.NullValue;

    /// <inheritdoc />
    public IReadOnlyList<IFilterFieldInfo> GetFields() => _value.GetFields();

    /// <inheritdoc />
    public IReadOnlyList<IFilterOperationInfo> GetOperations() => _value.GetOperations();

    /// <inheritdoc />
    public IDictionary<string, object?>? ToDictionary()
        => Serialize(_value) as IDictionary<string, object?>;

    public Expression<Func<T, bool>>? AsPredicate<T>()
    {
        var localContextData = _context.LocalContextData;

        if (localContextData.TryGetValue(ContextAsPredicateKey, out var asPredicateObj) &&
            asPredicateObj is AsPredicate<T> asPredicate)
        {
            return asPredicate(_context, false);
        }

        return null;
    }

    private static object? Serialize(IFilterValueNode? value)
    {
        switch (value)
        {
            case null:
                return null;

            case IFilterValueCollection collection:
                return collection.Select(Serialize).ToArray();

            case IFilterValue filterValue:
                return filterValue.Value;

            case IFilterInfo info:
                Dictionary<string, object?> data = new();

                foreach (var field in info.GetFields())
                {
                    SerializeAndAssign(field.Field.Name, field.Value);
                }

                foreach (var operation in info.GetOperations())
                {
                    SerializeAndAssign(operation.Field.Name, operation.Value);
                }

                return data;

                void SerializeAndAssign(string fieldName, IFilterValueNode? value)
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
