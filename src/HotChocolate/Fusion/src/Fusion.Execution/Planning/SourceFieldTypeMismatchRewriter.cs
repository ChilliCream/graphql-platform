using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal static class SourceFieldTypeMismatchRewriter
{
    public static SourceFieldTypeMismatchRewriteResult RewriteDynamic(
        OperationDefinitionNode operation,
        ITypeDefinition rootType,
        FusionSchemaDefinition schema)
    {
        var schemaNames = CollectRootSourceSchemaNames(operation.SelectionSet, rootType, schema);
        HashSet<FieldNode>? aliasedFields = null;

        foreach (var schemaName in schemaNames)
        {
            var result = Rewrite(operation, rootType, schemaName, schema);
            if (result.Aliases is null)
            {
                continue;
            }

            aliasedFields ??= new HashSet<FieldNode>(ReferenceEqualityComparer.Instance);
            aliasedFields.UnionWith(result.Aliases.Keys);
        }

        if (aliasedFields is null)
        {
            return new SourceFieldTypeMismatchRewriteResult(operation, null);
        }

        var aliases = CreateAliases(operation.SelectionSet, aliasedFields);
        var selectionSet = RewriteSelectionSet(operation.SelectionSet, aliases);
        return new SourceFieldTypeMismatchRewriteResult(
            operation.WithSelectionSet(selectionSet),
            aliases);
    }

    public static SourceFieldTypeMismatchRewriteResult Rewrite(
        OperationDefinitionNode operation,
        ITypeDefinition rootType,
        string schemaName,
        FusionSchemaDefinition schema)
    {
        var context = new Context(operation, schemaName, schema);
        var root = new SelectionSetContext(operation.SelectionSet, rootType);

        if (!context.HasSourceTypeDifference(root))
        {
            return new SourceFieldTypeMismatchRewriteResult(operation, null);
        }

        context.Analyze([root]);

        if (context.Aliases is null)
        {
            return new SourceFieldTypeMismatchRewriteResult(operation, null);
        }

        var selectionSet = RewriteSelectionSet(operation.SelectionSet, context.Aliases);
        return new SourceFieldTypeMismatchRewriteResult(
            operation.WithSelectionSet(selectionSet),
            context.Aliases);
    }

    private static SortedSet<string> CollectRootSourceSchemaNames(
        SelectionSetNode selectionSet,
        ITypeDefinition rootType,
        FusionSchemaDefinition schema)
    {
        var schemaNames = new SortedSet<string>(StringComparer.Ordinal);
        Collect(selectionSet, rootType);
        return schemaNames;

        void Collect(SelectionSetNode currentSelectionSet, ITypeDefinition parentType)
        {
            foreach (var selection in currentSelectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field
                        when parentType is FusionComplexTypeDefinition complexType
                        && complexType.Fields.TryGetField(
                            field.Name.Value,
                            allowInaccessibleFields: true,
                            out var fieldDefinition):
                        foreach (var source in fieldDefinition.Sources)
                        {
                            schemaNames.Add(source.SchemaName);
                        }
                        break;

                    case InlineFragmentNode inlineFragment:
                        var fragmentType = inlineFragment.TypeCondition is { } typeCondition
                            && schema.Types.TryGetType(
                                typeCondition.Name.Value,
                                allowInaccessibleFields: true,
                                out var conditionType)
                                ? conditionType
                                : parentType;
                        Collect(inlineFragment.SelectionSet, fragmentType);
                        break;
                }
            }
        }
    }

    private static Dictionary<FieldNode, string> CreateAliases(
        SelectionSetNode selectionSet,
        HashSet<FieldNode> aliasedFields)
    {
        var reservedResponseNames = new HashSet<string>(StringComparer.Ordinal);
        CollectResponseNames(selectionSet, reservedResponseNames);

        var aliases = new Dictionary<FieldNode, string>(ReferenceEqualityComparer.Instance);
        var nextAlias = 0;
        AddAliases(selectionSet);
        return aliases;

        void AddAliases(SelectionSetNode currentSelectionSet)
        {
            foreach (var selection in currentSelectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field:
                        if (aliasedFields.Contains(field))
                        {
                            string alias;
                            do
                            {
                                alias = $"fusion__field_{++nextAlias}";
                            }
                            while (!reservedResponseNames.Add(alias));

                            aliases.Add(field, alias);
                        }

                        if (field.SelectionSet is not null)
                        {
                            AddAliases(field.SelectionSet);
                        }
                        break;

                    case InlineFragmentNode inlineFragment:
                        AddAliases(inlineFragment.SelectionSet);
                        break;
                }
            }
        }
    }

    private static void CollectResponseNames(
        SelectionSetNode selectionSet,
        HashSet<string> responseNames)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    responseNames.Add(field.Alias?.Value ?? field.Name.Value);
                    if (field.SelectionSet is not null)
                    {
                        CollectResponseNames(field.SelectionSet, responseNames);
                    }
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectResponseNames(inlineFragment.SelectionSet, responseNames);
                    break;
            }
        }
    }

    private static SelectionSetNode RewriteSelectionSet(
        SelectionSetNode selectionSet,
        IReadOnlyDictionary<FieldNode, string> aliases)
    {
        List<ISelectionNode>? rewritten = null;

        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];
            var updated = selection;

            switch (selection)
            {
                case FieldNode field:
                {
                    var child = field.SelectionSet is null
                        ? null
                        : RewriteSelectionSet(field.SelectionSet, aliases);

                    if (aliases.TryGetValue(field, out var alias))
                    {
                        updated = new FieldNode(
                            field.Location,
                            field.Name,
                            new NameNode(alias),
                            field.Directives,
                            field.Arguments,
                            child);
                    }
                    else if (!ReferenceEquals(child, field.SelectionSet))
                    {
                        updated = field.WithSelectionSet(child);
                    }

                    break;
                }

                case InlineFragmentNode inlineFragment:
                {
                    var child = RewriteSelectionSet(inlineFragment.SelectionSet, aliases);
                    if (!ReferenceEquals(child, inlineFragment.SelectionSet))
                    {
                        updated = inlineFragment.WithSelectionSet(child);
                    }

                    break;
                }
            }

            if (!ReferenceEquals(updated, selection) && rewritten is null)
            {
                rewritten = [with(selectionSet.Selections.Count)];
                for (var j = 0; j < i; j++)
                {
                    rewritten.Add(selectionSet.Selections[j]);
                }
            }

            rewritten?.Add(updated);
        }

        return rewritten is null ? selectionSet : selectionSet.WithSelections(rewritten);
    }

    private static bool HasSameResponseShape(
        SourceOutputField firstSource,
        SourceOutputField secondSource)
    {
        var first = firstSource.Type;
        var second = secondSource.Type;

        while (true)
        {
            if (first is NonNullType || second is NonNullType)
            {
                if (first is not NonNullType || second is not NonNullType)
                {
                    return false;
                }

                first = first.InnerType();
                second = second.InnerType();
                continue;
            }

            if (first is ListType || second is ListType)
            {
                if (first is not ListType || second is not ListType)
                {
                    return false;
                }

                first = first.InnerType();
                second = second.InnerType();
                continue;
            }

            if (first.IsCompositeType() && second.IsCompositeType())
            {
                return true;
            }

            var firstTypeName = firstSource.SourceTypeName ?? first.NamedType().Name;
            var secondTypeName = secondSource.SourceTypeName ?? second.NamedType().Name;
            return firstTypeName.Equals(secondTypeName, StringComparison.Ordinal);
        }
    }

    private ref struct Context(
        OperationDefinitionNode operation,
        string schemaName,
        FusionSchemaDefinition schema)
    {
        private HashSet<string>? _reservedResponseNames;
        private int _nextAlias;

        public Dictionary<FieldNode, string>? Aliases { get; private set; }

        public bool HasSourceTypeDifference(SelectionSetContext selectionSetContext)
        {
            foreach (var selection in selectionSetContext.SelectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field when TryGetSourceField(
                        selectionSetContext.ParentType,
                        field.Name.Value,
                        out var fieldDefinition,
                        out var source):
                        if (source.SourceTypeName is not null
                            || !fieldDefinition.Type.Equals(source.Type, TypeComparison.Structural))
                        {
                            return true;
                        }

                        if (field.SelectionSet is not null
                            && source.Type.NamedType() is ITypeDefinition childType
                            && HasSourceTypeDifference(
                                new SelectionSetContext(field.SelectionSet, childType)))
                        {
                            return true;
                        }

                        break;

                    case InlineFragmentNode inlineFragment:
                        var parentType = ResolveTypeCondition(
                            inlineFragment,
                            selectionSetContext.ParentType);

                        if (HasSourceTypeDifference(
                            new SelectionSetContext(inlineFragment.SelectionSet, parentType)))
                        {
                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        public void Analyze(IReadOnlyList<SelectionSetContext> selectionSets)
        {
            Dictionary<string, List<FieldOccurrence>>? fields = null;

            for (var i = 0; i < selectionSets.Count; i++)
            {
                CollectFields(selectionSets[i], ref fields);
            }

            if (fields is null)
            {
                return;
            }

            foreach (var occurrences in fields.Values)
            {
                AnalyzeOccurrences(occurrences);
            }
        }

        private void AnalyzeOccurrences(List<FieldOccurrence> occurrences)
        {
            var first = occurrences[0];
            List<SelectionSetContext>? compatibleChildren = null;

            AddChild(first, ref compatibleChildren);

            for (var i = 1; i < occurrences.Count; i++)
            {
                var current = occurrences[i];

                if (HasSameResponseShape(first.Source, current.Source))
                {
                    AddChild(current, ref compatibleChildren);
                    continue;
                }

                AddAlias(current.Field);

                if (current.Field.SelectionSet is not null
                    && current.Source.Type.NamedType() is ITypeDefinition childType)
                {
                    Analyze([new SelectionSetContext(current.Field.SelectionSet, childType)]);
                }
            }

            if (compatibleChildren is not null)
            {
                Analyze(compatibleChildren);
            }
        }

        private static void AddChild(
            FieldOccurrence occurrence,
            ref List<SelectionSetContext>? children)
        {
            if (occurrence.Field.SelectionSet is not null
                && occurrence.Source.Type.NamedType() is ITypeDefinition childType)
            {
                children ??= [];
                children.Add(new SelectionSetContext(occurrence.Field.SelectionSet, childType));
            }
        }

        private void CollectFields(
            SelectionSetContext selectionSetContext,
            ref Dictionary<string, List<FieldOccurrence>>? fields)
        {
            foreach (var selection in selectionSetContext.SelectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode field when TryGetSourceField(
                        selectionSetContext.ParentType,
                        field.Name.Value,
                        out _,
                        out var source):
                        var responseName = field.Alias?.Value ?? field.Name.Value;
                        fields ??= new Dictionary<string, List<FieldOccurrence>>(StringComparer.Ordinal);

                        if (!fields.TryGetValue(responseName, out var occurrences))
                        {
                            occurrences = [];
                            fields.Add(responseName, occurrences);
                        }

                        occurrences.Add(new FieldOccurrence(field, source));
                        break;

                    case InlineFragmentNode inlineFragment:
                        var parentType = ResolveTypeCondition(
                            inlineFragment,
                            selectionSetContext.ParentType);

                        CollectFields(
                            new SelectionSetContext(inlineFragment.SelectionSet, parentType),
                            ref fields);
                        break;
                }
            }
        }

        private bool TryGetSourceField(
            ITypeDefinition parentType,
            string fieldName,
            out FusionOutputFieldDefinition field,
            out SourceOutputField source)
        {
            if (parentType is FusionComplexTypeDefinition complexType
                && complexType.Fields.TryGetField(
                    fieldName,
                    allowInaccessibleFields: true,
                    out var resolvedField)
                && resolvedField.Sources.TryGetMember(schemaName, out var resolvedSource))
            {
                field = resolvedField;
                source = resolvedSource;
                return true;
            }

            field = null!;
            source = null!;
            return false;
        }

        private ITypeDefinition ResolveTypeCondition(
            InlineFragmentNode inlineFragment,
            ITypeDefinition parentType)
            => inlineFragment.TypeCondition is { } typeCondition
                && schema.Types.TryGetType(
                    typeCondition.Name.Value,
                    allowInaccessibleFields: true,
                    out var conditionType)
                    ? conditionType
                    : parentType;

        private void AddAlias(FieldNode field)
        {
            Aliases ??= new Dictionary<FieldNode, string>(ReferenceEqualityComparer.Instance);

            if (Aliases.ContainsKey(field))
            {
                return;
            }

            _reservedResponseNames ??= CollectResponseNames();

            string alias;
            do
            {
                alias = $"fusion__field_{++_nextAlias}";
            }
            while (!_reservedResponseNames.Add(alias));

            Aliases.Add(field, alias);
        }

        private HashSet<string> CollectResponseNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            Collect(operation.SelectionSet);
            return names;

            void Collect(SelectionSetNode selectionSet)
            {
                foreach (var selection in selectionSet.Selections)
                {
                    switch (selection)
                    {
                        case FieldNode field:
                            names.Add(field.Alias?.Value ?? field.Name.Value);
                            if (field.SelectionSet is not null)
                            {
                                Collect(field.SelectionSet);
                            }
                            break;

                        case InlineFragmentNode inlineFragment:
                            Collect(inlineFragment.SelectionSet);
                            break;
                    }
                }
            }
        }
    }

    private readonly record struct SelectionSetContext(
        SelectionSetNode SelectionSet,
        ITypeDefinition ParentType);

    private readonly record struct FieldOccurrence(
        FieldNode Field,
        SourceOutputField Source);
}

internal readonly record struct SourceFieldTypeMismatchRewriteResult(
    OperationDefinitionNode Operation,
    Dictionary<FieldNode, string>? Aliases);
