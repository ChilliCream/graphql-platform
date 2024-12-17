using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

// TODO: We need to merge selections
public sealed class InlineFragmentOperationRewriter(CompositeSchema schema)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName)
    {
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var fragmentLookup = CreateFragmentLookup(document);
        var context = new Context(operationType, fragmentLookup);

        CollectSelections(operation.SelectionSet, context);
        RewriteSelections(context);

        var newSelectionSet = new SelectionSetNode(
            null,
            context.Selections.ToImmutable());

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentNode(ImmutableArray<IDefinitionNode>.Empty.Add(newOperation));
    }

    internal void CollectSelections(SelectionSetNode selectionSet, Context context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    context.AddField(field);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    CollectFragmentSpread(fragmentSpread, context);
                    break;
            }
        }
    }

    internal void RewriteSelections(Context context)
    {
        var collectedSelections = context.Selections.ToImmutableArray();
        context.Selections.Clear();

        foreach (var selection in collectedSelections)
        {
            switch (selection)
            {
                case FieldNode field:
                    MergeField(field.Name.Value, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    RewriteInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    InlineFragmentDefinition(fragmentSpread, context);
                    break;
            }
        }

        void MergeField(string fieldName, Context ctx)
        {
            foreach (var field in ctx.Fields[fieldName].GroupBy(t => t, t => t, FieldComparer.Instance))
            {
                var mergedField = field.Key;

                if (mergedField.SelectionSet is not null)
                {
                    mergedField = mergedField.WithSelectionSet(
                        new SelectionSetNode(
                            field.SelectMany(t => t.SelectionSet!.Selections).ToList()));
                }

                RewriteField(mergedField, ctx);
            }
        }
    }

    private void RewriteField(FieldNode fieldNode, Context context)
    {
        if (fieldNode.SelectionSet is null)
        {
            var node = fieldNode.WithLocation(null);

            if (context.Visited.Add(node))
            {
                context.Selections.Add(node);
            }
        }
        else
        {
            var field = ((CompositeComplexType)context.Type).Fields[fieldNode.Name.Value];
            var fieldContext = context.Branch(field.Type.NamedType());

            CollectSelections(fieldNode.SelectionSet, fieldContext);
            RewriteSelections(fieldContext);

            var newSelectionSetNode = new SelectionSetNode(
                null,
                fieldContext.Selections.ToImmutable());

            var newFieldNode = new FieldNode(
                null,
                fieldNode.Name,
                fieldNode.Alias,
                RewriteDirectives(fieldNode.Directives),
                RewriteArguments(fieldNode.Arguments),
                newSelectionSetNode);

            if (context.Visited.Add(newFieldNode))
            {
                context.Selections.Add(newFieldNode);
            }
        }
    }

    private void CollectInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        if ((inlineFragment.TypeCondition is null
                || inlineFragment.TypeCondition.Name.Value.Equals(context.Type.Name, StringComparison.Ordinal))
            && inlineFragment.Directives.Count == 0)
        {
            CollectSelections(inlineFragment.SelectionSet, context);
            return;
        }

        context.AddInlineFragment(inlineFragment);
    }

    private void RewriteInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        var typeCondition = inlineFragment.TypeCondition is null
            ? context.Type
            : schema.GetType(inlineFragment.TypeCondition.Name.Value);

        var inlineFragmentContext = context.Branch(typeCondition);

        CollectSelections(inlineFragment.SelectionSet, inlineFragmentContext);
        RewriteSelections(inlineFragmentContext);

        var newSelectionSetNode = new SelectionSetNode(
            null,
            inlineFragmentContext.Selections.ToImmutable());

        var newInlineFragment = new InlineFragmentNode(
            null,
            inlineFragment.TypeCondition,
            RewriteDirectives(inlineFragment.Directives),
            newSelectionSetNode);

        context.Selections.Add(newInlineFragment);
    }

    private void CollectFragmentSpread(
        FragmentSpreadNode fragmentSpread,
        Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.GetType(fragmentDefinition.TypeCondition.Name.Value);

        if (fragmentSpread.Directives.Count == 0
            && typeCondition.IsAssignableFrom(context.Type))
        {
            CollectSelections(fragmentDefinition.SelectionSet, context);
            return;
        }

        context.AddFragmentSpread(fragmentSpread);
    }

    private void InlineFragmentDefinition(
        FragmentSpreadNode fragmentSpread,
        Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.GetType(fragmentDefinition.TypeCondition.Name.Value);
        var fragmentContext = context.Branch(typeCondition);

        CollectSelections(fragmentDefinition.SelectionSet, fragmentContext);
        RewriteSelections(fragmentContext);

        var selectionSet = new SelectionSetNode(
            null,
            fragmentContext.Selections.ToImmutable());

        var inlineFragment = new InlineFragmentNode(
            null,
            new NamedTypeNode(typeCondition.Name),
            RewriteDirectives(fragmentSpread.Directives),
            selectionSet);

        if (context.Visited.Add(inlineFragment))
        {
            context.Selections.Add(inlineFragment);
        }
    }

    private IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        if (directives.Count == 1)
        {
            var directive = directives[0];
            var newDirective = new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
            return ImmutableArray<DirectiveNode>.Empty.Add(newDirective);
        }

        var buffer = new DirectiveNode[directives.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            var directive = directives[i];
            buffer[i] = new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
        }

        return ImmutableArray.Create(buffer);
    }

    private IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return arguments;
        }

        if (arguments.Count == 1)
        {
            return ImmutableArray<ArgumentNode>.Empty.Add(arguments[0].WithLocation(null));
        }

        var buffer = new ArgumentNode[arguments.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = arguments[i].WithLocation(null);
        }

        return ImmutableArray.Create(buffer);
    }

    private Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
    {
        var lookup = new Dictionary<string, FragmentDefinitionNode>();

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragmentDef)
            {
                lookup.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return lookup;
    }

    public readonly ref struct Context(
        ICompositeNamedType type,
        Dictionary<string, FragmentDefinitionNode> fragments)
    {
        public ICompositeNamedType Type { get; } = type;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public HashSet<ISelectionNode> Visited { get; } = new(SyntaxComparer.BySyntax);

        public Dictionary<string, List<FieldNode>> Fields { get; } = new(StringComparer.Ordinal);

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragments[name];

        public void AddField(FieldNode field)
        {
            if (!Fields.TryGetValue(field.Name.Value, out var fields))
            {
                fields = [];
                Fields.Add(field.Name.Value, fields);
                Selections.Add(field);
            }

            fields.Add(field);
        }

        public void AddInlineFragment(InlineFragmentNode inlineFragment)
        {
            Selections.Add(inlineFragment);
        }

        public void AddFragmentSpread(FragmentSpreadNode fragmentSpread)
        {
            Selections.Add(fragmentSpread);
        }

        public Context Branch(ICompositeNamedType type)
            => new(type, fragments);
    }

    private sealed class FieldComparer : IEqualityComparer<FieldNode>
    {
        public bool Equals(FieldNode? x, FieldNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return Equals(x.Alias, y.Alias)
                && x.Name.Equals(y.Name)
                && Equals(x.Directives, y.Directives)
                && Equals(x.Arguments, y.Arguments);
        }

        private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(FieldNode obj)
        {
            var hashCode = new HashCode();

            if (obj.Alias is not null)
            {
                hashCode.Add(obj.Alias.Value);
            }

            hashCode.Add(obj.Name.Value);

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            for (var i = 0; i < obj.Arguments.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Arguments[i]));
            }

            return hashCode.ToHashCode();
        }

        public static FieldComparer Instance { get; } = new();
    }
}

#if NET8_0
public class OrderedDictionary<TKey, TValue>
    : IDictionary<TKey, TValue>
        , IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly List<KeyValuePair<TKey, TValue>> _order;
    private readonly Dictionary<TKey, TValue> _map;

    public OrderedDictionary(IEqualityComparer<TKey> keyComparer)
    {
        _order = [];
        _map = new Dictionary<TKey, TValue>(keyComparer);
    }

    public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        _order = [];
        _map = new Dictionary<TKey, TValue>();

        foreach (var item in values)
        {
            _map.Add(item.Key, item.Value);
            _order.Add(item);
        }
    }

    private OrderedDictionary(OrderedDictionary<TKey, TValue> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        _order = [..source._order,];
        _map = new Dictionary<TKey, TValue>(source._map);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _map.TryGetValue(key, out value);

    public TValue this[TKey key]
    {
        get
        {
            return _map[key];
        }
        set
        {
            if (_map.ContainsKey(key))
            {
                _map[key] = value;
                _order[IndexOfKey(key)] =
                    new KeyValuePair<TKey, TValue>(key, value);
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public ICollection<TKey> Keys => _map.Keys;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        Keys;

    public ICollection<TValue> Values => _map.Values;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        Values;

    public int Count => _order.Count;

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        _map.Add(item.Key, item.Value);
        _order.Add(item);
    }

    public void Clear()
    {
        _map.Clear();
        _order.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _order.Contains(item);

    public bool ContainsKey(TKey key)
        => _map.ContainsKey(key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => _order.CopyTo(array, arrayIndex);

    public bool Remove(TKey key)
    {
        if (_map.ContainsKey(key))
        {
            _map.Remove(key);
            _order.RemoveAt(IndexOfKey(key));
            return true;
        }

        return false;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        var index = _order.IndexOf(item);

        if (index != -1)
        {
            _order.RemoveAt(index);
            _map.Remove(item.Key);
            return true;
        }

        return false;
    }

    private int IndexOfKey(TKey key)
    {
        for (var i = 0; i < _order.Count; i++)
        {
            if (key.Equals(_order[i].Key))
            {
                return i;
            }
        }

        return -1;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        => _order.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _order.GetEnumerator();

    public OrderedDictionary<TKey, TValue> Clone() => new(this);
}
#endif
