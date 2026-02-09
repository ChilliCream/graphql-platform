using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.FusionUtilitiesResources;

namespace HotChocolate.Fusion.Rewriters;

public sealed class InlineFragmentOperationRewriter(
    ISchemaDefinition schema,
    bool removeStaticallyExcludedSelections = false,
    bool ignoreMissingTypeSystemMembers = false,
    bool includeTypeNameToEmptySelectionSets = true)
{
    private List<ISelectionNode>? _selections;

    private static readonly FieldNode s_typeNameField =
        new FieldNode(
            null,
            new NameNode(IntrospectionFieldNames.TypeName),
            null,
            [new DirectiveNode("fusion__empty")],
            ImmutableArray<ArgumentNode>.Empty,
            null);

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
                    if (!removeStaticallyExcludedSelections || IsIncluded(field.Directives))
                    {
                        context.AddField(field);
                    }
                    break;

                case InlineFragmentNode inlineFragment:
                    if (!removeStaticallyExcludedSelections || IsIncluded(inlineFragment.Directives))
                    {
                        CollectInlineFragment(inlineFragment, context);
                    }
                    break;

                case FragmentSpreadNode fragmentSpread:
                    if (!removeStaticallyExcludedSelections || IsIncluded(fragmentSpread.Directives))
                    {
                        CollectFragmentSpread(fragmentSpread, context);
                    }
                    break;
            }
        }
    }

    internal void RewriteSelections(Context context)
    {
        if (includeTypeNameToEmptySelectionSets && context.Selections.Count == 0)
        {
            context.Selections.Add(s_typeNameField);
            context.Fields.Add(IntrospectionFieldNames.TypeName, [s_typeNameField]);
        }

        var collectedSelections = context.Selections.ToArray();
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
                    ctx.Observer.OnMerge(field);
                    var temp = Interlocked.Exchange(ref _selections, null) ?? [];
                    temp.AddRange(field.SelectMany(t => t.SelectionSet!.Selections));
                    var selections = temp.ToArray();
                    temp.Clear();
                    Interlocked.Exchange(ref _selections, temp);
                    mergedField = mergedField.WithSelectionSet(new SelectionSetNode(selections));
                }

                if (removeStaticallyExcludedSelections)
                {
                    var directives = RemoveStaticIncludeConditions(mergedField.Directives);
                    mergedField = mergedField.WithDirectives(directives);
                }

                ctx.Observer.OnMerge(field.Key, mergedField);
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
            var type = (IComplexTypeDefinition)context.Type;
            ITypeDefinition fieldType;

            if (type.Fields.TryGetField(fieldNode.Name.Value, out var field))
            {
                fieldType = field.Type.AsTypeDefinition();
            }
            else if (ignoreMissingTypeSystemMembers)
            {
                fieldType = new MissingType("__MissingType__");
            }
            else
            {
                throw new RewriterException(
                    string.Format(
                        InlineFragmentOperationRewriter_FieldDoesNotExistOnType,
                        fieldNode.Name.Value,
                        type.Name));
            }

            var fieldContext = context.Branch(fieldType);

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

            context.Observer.OnMerge(fieldNode, newFieldNode);

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
        ITypeDefinition? typeCondition;
        if (inlineFragment.TypeCondition is null)
        {
            typeCondition = context.Type;
        }
        else
        {
            var typeName = inlineFragment.TypeCondition.Name.Value;

            if (!schema.Types.TryGetType(typeName, out typeCondition))
            {
                if (ignoreMissingTypeSystemMembers)
                {
                    typeCondition = new MissingType("__MissingType__");
                }
                else
                {
                    throw new RewriterException(string.Format(
                        InlineFragmentOperationRewriter_InvalidTypeConditionOnInlineFragment,
                        context.Type.Name,
                        typeName));
                }
            }
        }

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

        context.Observer.OnMerge(inlineFragment, newInlineFragment);
        context.Selections.Add(newInlineFragment);
    }

    private void CollectFragmentSpread(
        FragmentSpreadNode fragmentSpread,
        Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeName = fragmentDefinition.TypeCondition.Name.Value;

        if (!schema.Types.TryGetType(typeName, out var typeCondition))
        {
            if (ignoreMissingTypeSystemMembers)
            {
                typeCondition = new MissingType("__MissingType__");
            }
            else
            {
                throw new RewriterException(string.Format(
                    InlineFragmentOperationRewriter_InvalidTypeConditionOnFragment,
                    fragmentSpread.Name,
                    typeName));
            }
        }

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

        context.Observer.OnMerge(fragmentDefinition.SelectionSet, inlineFragment.SelectionSet);

        if (context.Visited.Add(inlineFragment))
        {
            context.Selections.Add(inlineFragment);
        }
    }

    private static IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
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

    private static IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
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

    private static Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
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

    private static bool IsIncluded(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return true;
        }

        var skipChecked = false;
        var includeChecked = false;

        if (directives.Count == 1)
        {
            return TryCheckIsIncluded(directives[0], ref skipChecked, ref includeChecked);
        }

        if (directives.Count == 2)
        {
            return TryCheckIsIncluded(directives[0], ref skipChecked, ref includeChecked)
                && TryCheckIsIncluded(directives[1], ref skipChecked, ref includeChecked);
        }

        if (directives.Count == 3)
        {
            var included = TryCheckIsIncluded(directives[0], ref skipChecked, ref includeChecked);

            if (!included)
            {
                return false;
            }

            included = TryCheckIsIncluded(directives[1], ref skipChecked, ref includeChecked);

            if (!included)
            {
                return false;
            }
            else if (skipChecked && includeChecked)
            {
                return true;
            }

            return TryCheckIsIncluded(directives[2], ref skipChecked, ref includeChecked);
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var included = TryCheckIsIncluded(directives[i], ref skipChecked, ref includeChecked);

            if (!included)
            {
                return false;
            }
            else if (skipChecked && includeChecked)
            {
                return true;
            }
        }

        return true;
    }

    private static bool TryCheckIsIncluded(DirectiveNode directive, ref bool skipChecked, ref bool includeChecked)
    {
        if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
        {
            skipChecked = true;

            if (directive.Arguments is [{ Value: BooleanValueNode { Value: true } }])
            {
                return false;
            }
        }
        else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
        {
            includeChecked = true;

            if (directive.Arguments is [{ Value: BooleanValueNode { Value: false } }])
            {
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<DirectiveNode> RemoveStaticIncludeConditions(
        IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        var skipChecked = false;
        var includeChecked = false;

        if (directives.Count == 1)
        {
            var directive = directives[0];
            return IsStaticIncludeCondition(directive, ref skipChecked, ref includeChecked) ? [] : directives;
        }

        if (directives.Count == 2)
        {
            var directive1 = directives[0];
            var directive2 = directives[1];

            var remove1 = IsStaticIncludeCondition(directive1, ref skipChecked, ref includeChecked);

            if (IsStaticIncludeCondition(directive2, ref skipChecked, ref includeChecked))
            {
                return remove1 ? [] : [directive1];
            }

            return remove1 ? [directive2] : directives;
        }

        if (directives.Count == 3)
        {
            var directive1 = directives[0];
            var directive2 = directives[1];
            var directive3 = directives[2];

            var remove1 = IsStaticIncludeCondition(directive1, ref skipChecked, ref includeChecked);
            var remove2 = IsStaticIncludeCondition(directive2, ref skipChecked, ref includeChecked);
            var remove3 =
                (skipChecked && includeChecked)
                || IsStaticIncludeCondition(directive3, ref skipChecked, ref includeChecked);

            switch (remove1)
            {
                case true when remove2 && remove3:
                    return [];

                case false when !remove2 && !remove3:
                    return directives;

                default:
                    var list = new List<DirectiveNode>();

                    if (!remove1)
                    {
                        list.Add(directive1);
                    }

                    if (!remove2)
                    {
                        list.Add(directive2);
                    }

                    if (!remove3)
                    {
                        list.Add(directive3);
                    }

                    return list;
            }
        }

        List<DirectiveNode>? result = null;

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if ((skipChecked && includeChecked)
                || IsStaticIncludeCondition(directive, ref skipChecked, ref includeChecked))
            {
                if (result is not null)
                {
                    continue;
                }

                result = [];
                for (var j = 0; j < i; j++)
                {
                    result.Add(directives[j]);
                }
            }
            else
            {
                result?.Add(directive);
            }
        }

        if (result is null)
        {
            return directives;
        }

        return result.Count == 0 ? [] : result;

        static bool IsStaticIncludeCondition(DirectiveNode directive, ref bool skipChecked, ref bool includeChecked)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
            {
                skipChecked = true;
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    return true;
                }
            }
            else if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
            {
                includeChecked = true;
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public readonly ref struct Context(
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragments,
        ISelectionSetMergeObserver? mergeObserver = null)
    {
        public ITypeDefinition Type { get; } = type;

        public ISelectionSetMergeObserver Observer { get; } =
            mergeObserver ?? NoopSelectionSetMergeObserver.Instance;

        public ImmutableArray<ISelectionNode>.Builder Selections { get; } =
            ImmutableArray.CreateBuilder<ISelectionNode>();

        public HashSet<ISelectionNode> Visited { get; } = new(SyntaxComparer.BySyntax);

        public Dictionary<string, List<FieldNode>> Fields { get; } = new(StringComparer.Ordinal);

        public FragmentDefinitionNode GetFragmentDefinition(string name)
        {
            if (!fragments.TryGetValue(name, out var fragment))
            {
                throw new RewriterException(string.Format(
                    InlineFragmentOperationRewriter_FragmentDoesNotExist,
                    name));
            }

            return fragment;
        }

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
            => new(type, fragments, Observer);
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
