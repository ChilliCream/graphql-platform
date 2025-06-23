using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.Sorting.Expressions.QueryableSortProvider;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Encapsulated all sorting-specific information
/// </summary>
public class SortingContext : ISortingContext
{
    private static readonly MethodInfo s_createSortByMethod =
        typeof(SortingContext).GetMethod(nameof(CreateSortBy), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> s_sortByFactoryCache = new();
    private static readonly SortDefinitionFormatter s_formatter = new();
    private readonly IReadOnlyList<SortingInfo> _value;
    private readonly IResolverContext _context;
    private readonly IType _type;
    private readonly IValueNode _valueNode;

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
            : [new SortingInfo(type, valueNode, inputParser)];
        _context = context;
        _type = type;
        _valueNode = valueNode;
    }

    /// <inheritdoc />
    public void Handled(bool isHandled)
    {
        _context.LocalContextData = isHandled
            ? _context.LocalContextData.SetItem(SkipSortingKey, true)
            : _context.LocalContextData.Remove(SkipSortingKey);
    }

    /// <inheritdoc />
    public bool IsDefined => _value is not [{ ValueNode.Kind: SyntaxKind.NullValue }];

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
                Dictionary<string, object?> data = [];

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

    public SortDefinition<T>? AsSortDefinition<T>()
    {
        if(_valueNode.Kind == SyntaxKind.NullValue
            || _valueNode is ListValueNode { Items.Count: 0 }
            || _valueNode is ObjectValueNode { Fields.Count: 0 })
        {
            return null;
        }

        var builder = ImmutableArray.CreateBuilder<ISortBy<T>>();
        var parameter = Expression.Parameter(typeof(T), "t");

        foreach (var (selector, ascending, type) in s_formatter.Rewrite(_valueNode, _type, parameter))
        {
            var factory = s_sortByFactoryCache.GetOrAdd(
                (typeof(T), type),
                static key => s_createSortByMethod.MakeGenericMethod(key.Item1, key.Item2));
            var sortBy = (ISortBy<T>)factory.Invoke(null, [parameter, selector, ascending])!;
            builder.Add(sortBy);
        }

        return new SortDefinition<T>(builder.ToImmutable());
    }

    private static SortBy<TEntity, TValue> CreateSortBy<TEntity, TValue>(
        ParameterExpression parameter,
        Expression selector,
        bool ascending)
        => new(Expression.Lambda<Func<TEntity, TValue>>(selector, parameter), ascending);

    private sealed class SortDefinitionFormatter : SyntaxWalker<SortDefinitionFormatter.Context>
    {
        public IEnumerable<(Expression, bool, Type)> Rewrite(IValueNode node, IType type, Expression parameter)
        {
            var context = new Context();
            context.Types.Push((InputObjectType)type);
            context.Parents.Push(parameter);
            Visit(node, context);
            return context.Completed;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            Context context)
        {
            var type = context.Types.Peek();
            var field = (SortField)type.Fields[node.Name.Value];
            var fieldType = field.Type.NamedType();

            var parent = context.Parents.Peek();
            context.Parents.Push(Expression.Property(parent, (PropertyInfo)field.Member!));

            if (fieldType.IsInputObjectType())
            {
                context.Types.Push((InputObjectType)fieldType);
            }

            return base.Leave(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            Context context)
        {
            var type = context.Types.Peek();

            if (type.Fields.TryGetField(node.Name.Value, out var inputField) && inputField is SortField sortField)
            {
                var fieldType = sortField.Type.NamedType();
                var expression = context.Parents.Pop();

                if (fieldType.IsInputObjectType())
                {
                    context.Types.Pop();
                }
                else
                {
                    var ascending = node.Value.Value?.Equals("ASC") ?? true;
                    context.Completed.Add((expression, ascending, sortField.Member!.GetReturnType()));
                }
            }
            else
            {
                context.Types.Pop();
                context.Parents.Pop();
            }

            return base.Leave(node, context);
        }

        public class Context
        {
            public Stack<InputObjectType> Types { get; } = new();

            public Stack<Expression> Parents { get; } = new();

            public List<(Expression, bool, Type)> Completed { get; } = [];
        }
    }
}
