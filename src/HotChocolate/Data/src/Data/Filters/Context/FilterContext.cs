using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Filters.Expressions.QueryableFilterProvider;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Encapuslates all filter specific information
/// </summary>
public class FilterContext : FilterValue, IFilterContext
{
    private readonly IResolverContext _context;

    /// <summary>
    /// Creates a new instance of <see cref="FilterContext" />
    /// </summary>
    public FilterContext(
        IResolverContext context,
        IType type,
        IValueNode valueNode,
        InputParser inputParser)
        : base(type, valueNode, inputParser)
    {
        _context = context;
    }

    /// <inheritdoc />
    public void EnableFilterExecution(bool enable = true)
    {
        if (enable)
        {
            _context.LocalContextData = _context.LocalContextData.Remove(SkipFilteringKey);
        }
        else
        {
            _context.LocalContextData = _context.LocalContextData.SetItem(SkipFilteringKey, true);
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object?>? ToDictionary()
        => Serialize(this) as IDictionary<string, object?>;

    private object? Serialize(IFilterValueInfo? value)
    {
        switch (value)
        {
            case null:
                return null;

            case IFilterValueCollection collection:
                return collection.Select(Serialize).ToArray();

            case IFilterValue v:
                Dictionary<string, object?> data = new();

                foreach (var field in v.GetFields())
                {
                    SerializeAndAssign(field.Field.Name, field.Value);
                }

                foreach (var operation in v.GetOperations())
                {
                    SerializeAndAssign(operation.Field.Name, operation.Value);
                }

                return data;

                void SerializeAndAssign(string fieldName, IFilterValueInfo? value)
                {
                    if (value is null)
                    {
                        data[fieldName] = null;
                    }
                    else if (value.Type.NamedType().IsScalarType() &&
                       value is IFilterValue filterValue)
                    {
                        data[fieldName] = filterValue.ParseValue();
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
