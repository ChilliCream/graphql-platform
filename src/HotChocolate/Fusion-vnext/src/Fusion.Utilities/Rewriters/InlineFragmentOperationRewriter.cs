using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class InlineFragmentOperationRewriter(ISchemaDefinition schema)
{
    public DocumentNode RewriteDocument(DocumentNode document, string? operationName = null)
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
            operation.Description,
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
                    MergeField(field.Alias?.Value ?? field.Name.Value, context);
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
            var field = ((IComplexTypeDefinition)context.Type).Fields[fieldNode.Name.Value];
            var fieldContext = context.Branch(field.Type.AsTypeDefinition());

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
            : schema.Types[inlineFragment.TypeCondition.Name.Value];

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
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];

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
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];
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
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragments)
    {
        public ITypeDefinition Type { get; } = type;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public HashSet<ISelectionNode> Visited { get; } = new(SyntaxComparer.BySyntax);

        public Dictionary<string, List<FieldNode>> Fields { get; } = new(StringComparer.Ordinal);

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragments[name];

        public void AddField(FieldNode field)
        {
            var responseName = field.Alias?.Value ?? field.Name.Value;
            if (!Fields.TryGetValue(responseName, out var fields))
            {
                fields = [];
                Fields.Add(responseName, fields);
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

        public Context Branch(ITypeDefinition type)
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
