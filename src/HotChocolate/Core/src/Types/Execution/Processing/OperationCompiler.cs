using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Features;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    private readonly Schema _schema;
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldsPool;
    private readonly OperationCompilerOptimizers _optimizers;
    private readonly InlineFragmentOperationRewriter _documentRewriter;
    private readonly InputParser _inputValueParser;
    private static readonly ArrayPool<object> s_objectArrayPool = ArrayPool<object>.Shared;

    internal OperationCompiler(
        Schema schema,
        InputParser inputValueParser,
        ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> fieldsPool,
        OperationCompilerOptimizers optimizers)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fieldsPool);

        _schema = schema;
        _inputValueParser = inputValueParser;
        _fieldsPool = fieldsPool;
        _documentRewriter = new InlineFragmentOperationRewriter(
            schema,
            removeStaticallyExcludedSelections: true,
            includeTypeNameToEmptySelectionSets: false);
        _optimizers = optimizers;
    }

    public static Operation Compile(
        string id,
        DocumentNode document,
        Schema schema,
        IFeatureProvider? context = null)
        => Compile(id, id, null, document, schema, context);

    public static Operation Compile(
        string id,
        string? operationName,
        DocumentNode document,
        Schema schema,
        IFeatureProvider? context = null)
        => Compile(id, id, operationName, document, schema, context);

    public static Operation Compile(
        string id,
        string hash,
        string? operationName,
        DocumentNode document,
        Schema schema,
        IFeatureProvider? context = null)
        => new OperationCompiler(
            schema,
            new InputParser(),
            new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
                new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>()),
            new OperationCompilerOptimizers())
            .Compile(id, hash, operationName, document, context ?? EmptyFeatureProvider.Instance);

    public Operation Compile(
        string id,
        string hash,
        string? operationName,
        DocumentNode document,
        IFeatureProvider context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(document);

        // Before we can plan an operation, we must de-fragmentize it and remove static include conditions.
        var result = _documentRewriter.RewriteDocument(document, operationName);
        document = result.Document;
        var operationDefinition = document.GetOperation(operationName);

        var includeConditions = new IncludeConditionCollection();
        var deferConditions = new DeferConditionCollection();
        IncludeConditionVisitor.Instance.Visit(operationDefinition, includeConditions);
        DeferConditionVisitor.Instance.Visit(operationDefinition, deferConditions);
        var fields = _fieldsPool.Get();

        var compilationContext = new CompilationContext(s_objectArrayPool.Rent(128));

        try
        {
            var lastId = 0;
            const ulong parentIncludeFlags = 0ul;
            var rootType = _schema.GetOperationType(operationDefinition.Operation);

            CollectFields(
                parentIncludeFlags,
                operationDefinition.SelectionSet.Selections,
                rootType,
                fields,
                includeConditions,
                deferConditions,
                parentDeferUsage: null);

            var selectionSet = BuildSelectionSet(
                SelectionPath.Root,
                fields,
                rootType,
                compilationContext,
                _optimizers.SelectionSetOptimizers,
                ref lastId);

            compilationContext.Register(selectionSet, selectionSet.Id);

            var operation = new Operation(
                id,
                hash,
                document,
                operationDefinition,
                rootType,
                _schema,
                selectionSet,
                compiler: this,
                includeConditions,
                deferConditions,
                compilationContext.Features,
                lastId,
                compilationContext.ElementsById,
                hasIncrementalParts: result.HasIncrementalParts);

            selectionSet.Complete(operation);

            if (_optimizers.OperationOptimizers.Length > 0)
            {
                var optimizerContext = new OperationOptimizerContext(operation);
                foreach (var optimizer in _optimizers.OperationOptimizers)
                {
                    optimizer.OptimizeOperation(optimizerContext);
                }
            }

            return operation;
        }
        finally
        {
            _fieldsPool.Return(fields);
        }
    }

    internal SelectionSet CompileSelectionSet(
        Operation operation,
        Selection selection,
        ObjectType objectType,
        IncludeConditionCollection includeConditions,
        DeferConditionCollection deferConditions,
        ref object[] elementsById,
        ref int lastId)
    {
        var compilationContext = new CompilationContext(elementsById, operation.Features);
        var optimizers = OperationCompilerOptimizerHelper.GetOptimizers(selection);

        var fields = _fieldsPool.Get();
        fields.Clear();

        try
        {
            var nodes = selection.SyntaxNodes;
            var first = nodes[0];

            CollectFields(
                first.PathIncludeFlags,
                first.Node.SelectionSet!.Selections,
                objectType,
                fields,
                includeConditions,
                deferConditions,
                parentDeferUsage: first.DeferUsage);

            if (nodes.Length > 1)
            {
                for (var i = 1; i < nodes.Length; i++)
                {
                    var node = nodes[i];

                    CollectFields(
                        node.PathIncludeFlags,
                        node.Node.SelectionSet!.Selections,
                        objectType,
                        fields,
                        includeConditions,
                        deferConditions,
                        parentDeferUsage: nodes[i].DeferUsage);
                }
            }

            var path = selection.DeclaringSelectionSet.Path.Append(selection.ResponseName);
            var selectionSet = BuildSelectionSet(path, fields, objectType, compilationContext, optimizers, ref lastId);
            compilationContext.Register(selectionSet, selectionSet.Id);
            elementsById = compilationContext.ElementsById;
            selectionSet.Complete(operation);
            return selectionSet;
        }
        finally
        {
            _fieldsPool.Return(fields);
        }
    }

    private void CollectFields(
        ulong parentIncludeFlags,
        IReadOnlyList<ISelectionNode> selections,
        IObjectTypeDefinition typeContext,
        OrderedDictionary<string, List<FieldSelectionNode>> fields,
        IncludeConditionCollection includeConditions,
        DeferConditionCollection deferConditions,
        DeferUsage? parentDeferUsage)
    {
        for (var i = 0; i < selections.Count; i++)
        {
            var selection = selections[i];

            if (selection is FieldNode fieldNode)
            {
                var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;
                var pathIncludeFlags = parentIncludeFlags;

                if (!fields.TryGetValue(responseName, out var nodes))
                {
                    nodes = [];
                    fields.Add(responseName, nodes);
                }

                if (IncludeCondition.TryCreate(fieldNode, out var includeCondition))
                {
                    var index = includeConditions.IndexOf(includeCondition);
                    pathIncludeFlags |= 1ul << index;
                }

                nodes.Add(new FieldSelectionNode(fieldNode, pathIncludeFlags, parentDeferUsage));
            }
            else if (selection is InlineFragmentNode inlineFragmentNode
                && DoesTypeApply(inlineFragmentNode.TypeCondition, typeContext))
            {
                var pathIncludeFlags = parentIncludeFlags;

                if (IncludeCondition.TryCreate(inlineFragmentNode, out var includeCondition))
                {
                    var index = includeConditions.IndexOf(includeCondition);
                    pathIncludeFlags |= 1ul << index;
                }

                var newDeferUsage = parentDeferUsage;

                if (DeferCondition.TryCreate(inlineFragmentNode, out var deferCondition))
                {
                    deferConditions.Add(deferCondition);
                    var deferIndex = deferConditions.IndexOf(deferCondition);
                    var label = GetDeferLabel(inlineFragmentNode);
                    newDeferUsage = new DeferUsage(label, parentDeferUsage, (byte)deferIndex);
                }

                CollectFields(
                    pathIncludeFlags,
                    inlineFragmentNode.SelectionSet.Selections,
                    typeContext,
                    fields,
                    includeConditions,
                    deferConditions,
                    newDeferUsage);
            }
        }
    }

    private SelectionSet BuildSelectionSet(
        SelectionPath path,
        OrderedDictionary<string, List<FieldSelectionNode>> fieldMap,
        ObjectType typeContext,
        CompilationContext compilationContext,
        ImmutableArray<ISelectionSetOptimizer> optimizers,
        ref int lastId)
    {
        var i = 0;
        var selections = new Selection[fieldMap.Count];
        var isConditional = false;
        var hasDeferredSelections = false;
        var includeFlags = new List<ulong>();
        var deferUsages = new List<DeferUsage>();
        var selectionSetId = ++lastId;
        var alwaysIncluded = false;

        foreach (var (responseName, nodes) in fieldMap)
        {
            includeFlags.Clear();
            deferUsages.Clear();

            var first = nodes[0];
            var isInternal = IsInternal(first.Node);
            var hasNonDeferredNode = first.DeferUsage is null;

            if (first.PathIncludeFlags == 0)
            {
                alwaysIncluded = true;
            }
            else
            {
                includeFlags.Add(first.PathIncludeFlags);
            }

            if (first.DeferUsage is not null)
            {
                deferUsages.Add(first.DeferUsage);
            }

            if (nodes.Count > 1)
            {
                for (var j = 1; j < nodes.Count; j++)
                {
                    var next = nodes[j];

                    if (!first.Node.Name.Value.Equals(next.Node.Name.Value, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"The syntax nodes for the response name {responseName} are not all the same.");
                    }

                    if (next.PathIncludeFlags == 0)
                    {
                        alwaysIncluded = true;
                        if (includeFlags.Count > 0)
                        {
                            includeFlags.Clear();
                        }
                    }
                    else if (!alwaysIncluded)
                    {
                        includeFlags.Add(next.PathIncludeFlags);
                    }

                    if (next.DeferUsage is null)
                    {
                        hasNonDeferredNode = true;
                    }
                    else if (!hasNonDeferredNode)
                    {
                        deferUsages.Add(next.DeferUsage);
                    }

                    if (isInternal)
                    {
                        isInternal = IsInternal(next.Node);
                    }
                }
            }

            if (includeFlags.Count > 1)
            {
                CollapseIncludeFlags(includeFlags);
            }

            // If any field node is not inside a deferred fragment, the selection
            // is not deferred — it must be included in the initial response.
            DeferUsage[]? finalDeferUsage = null;
            ulong deferMask = 0;

            if (!hasNonDeferredNode && deferUsages.Count > 0)
            {
                // Remove child defer usages when their parent is also in the set.
                // A field should be delivered with the outermost (earliest) defer
                // that contains it.
                for (var j = deferUsages.Count - 1; j >= 0; j--)
                {
                    var parent = deferUsages[j].Parent;
                    while (parent is not null)
                    {
                        if (deferUsages.Contains(parent))
                        {
                            deferUsages.RemoveAt(j);
                            break;
                        }

                        parent = parent.Parent;
                    }
                }

                finalDeferUsage = deferUsages.ToArray();
                foreach (var usage in deferUsages)
                {
                    deferMask |= 1ul << usage.DeferConditionIndex;
                }
                hasDeferredSelections = true;
            }

            if (!typeContext.Fields.TryGetField(first.Node.Name.Value, out var field))
            {
                throw ThrowHelper.FieldDoesNotExistOnType(first.Node, typeContext.Name);
            }
            var fieldDelegate = CreateFieldPipeline(_schema, field, first.Node);
            var pureFieldDelegate = TryCreatePureField(_schema, field, first.Node);
            var arguments = ArgumentMap.Empty;

            if (field.Arguments.Count > 0)
            {
                arguments = CoerceArgumentValues(field, first.Node);
            }

            var selection = new Selection(
                ++lastId,
                responseName,
                field,
                nodes.ToArray(),
                includeFlags.Count > 0 ? includeFlags.ToArray() : [],
                deferUsage: finalDeferUsage,
                deferMask: deferMask,
                isInternal: isInternal,
                arguments: arguments,
                resolverPipeline: fieldDelegate,
                pureResolver: pureFieldDelegate);

            if (optimizers.Length > 0)
            {
                var features = new SelectionFeatureCollection(compilationContext.Features, selection.Id);
                features.SetSafe(optimizers);
            }

            // Register the selection in the elements array
            compilationContext.Register(selection, selection.Id);
            selections[i++] = selection;

            if (includeFlags.Count > 0)
            {
                isConditional = true;
            }
        }

        // if there are no optimizers registered for this selection we exit early.
        if (optimizers.Length == 0)
        {
            return new SelectionSet(selectionSetId, path, typeContext, selections, isConditional, hasDeferredSelections);
        }

        var current = ImmutableCollectionsMarshal.AsImmutableArray(selections);
        var rewritten = current;

        var optimizerContext = new SelectionSetOptimizerContext(
            path,
            typeContext,
            ref rewritten,
            compilationContext.Features,
            ref lastId,
            _schema,
            CreateFieldPipeline);

        foreach (var optimizer in optimizers)
        {
            optimizer.OptimizeSelectionSet(optimizerContext);
        }

        // If `rewritten` is still the same instance as `current`,
        // the optimizers did not change the selections array.
        // This mean we can simply construct the SelectionSet.
        if (current == rewritten)
        {
            return new SelectionSet(selectionSetId, path, typeContext, selections, isConditional, hasDeferredSelections);
        }

        if (current.Length < rewritten.Length)
        {
            for (var j = current.Length; j < rewritten.Length; j++)
            {
                var selection = rewritten[j];

                if (optimizers.Length > 0)
                {
                    var features = new SelectionFeatureCollection(compilationContext.Features, selection.Id);
                    features.SetSafe(optimizers);
                }

                compilationContext.Register(selection, selection.Id);
            }
        }

        selections = ImmutableCollectionsMarshal.AsArray(rewritten)!;
        return new SelectionSet(selectionSetId, path, typeContext, selections, isConditional, hasDeferredSelections);
    }

    private static void CollapseIncludeFlags(List<ulong> includeFlags)
    {
        // we sort the include flags to improve early elimination and stability
        includeFlags.Sort();

        var write = 0;

        for (var read = 0; read < includeFlags.Count; read++)
        {
            var candidate = includeFlags[read];
            var covered = false;

            // we check if the candidate is already covered
            for (var i = 0; i < write; i++)
            {
                if ((candidate & includeFlags[i]) == includeFlags[i])
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
            {
                // lastly we remove more restrictive flags from the already written range
                for (var i = 0; i < write;)
                {
                    if ((includeFlags[i] & candidate) == candidate)
                    {
                        includeFlags[i] = includeFlags[--write];
                    }
                    else
                    {
                        i++;
                    }
                }

                if (write < read)
                {
                    includeFlags[write] = candidate;
                }
                write++;
            }
        }

        // we trim the list to the collapsed set
        if (write < includeFlags.Count)
        {
            includeFlags.RemoveRange(write, includeFlags.Count - write);
        }
    }

    private bool DoesTypeApply(NamedTypeNode? typeCondition, IObjectTypeDefinition typeContext)
    {
        if (typeCondition is null)
        {
            return true;
        }

        if (typeCondition.Name.Value.Equals(typeContext.Name, StringComparison.Ordinal))
        {
            return true;
        }

        if (_schema.Types.TryGetType(typeCondition.Name.Value, out var type))
        {
            return type.IsAssignableFrom(typeContext);
        }

        return false;
    }

    private static bool IsInternal(FieldNode fieldNode)
    {
        const string isInternal = "fusion__requirement";
        var directives = fieldNode.Directives;

        if (directives.Count == 0)
        {
            return false;
        }

        if (directives.Count == 1)
        {
            return directives[0].Name.Value.Equals(isInternal, StringComparison.Ordinal);
        }

        if (directives.Count == 2)
        {
            return directives[0].Name.Value.Equals(isInternal, StringComparison.Ordinal)
                || directives[1].Name.Value.Equals(isInternal, StringComparison.Ordinal);
        }

        if (directives.Count == 3)
        {
            return directives[0].Name.Value.Equals(isInternal, StringComparison.Ordinal)
                || directives[1].Name.Value.Equals(isInternal, StringComparison.Ordinal)
                || directives[2].Name.Value.Equals(isInternal, StringComparison.Ordinal);
        }

        for (var i = 0; i < directives.Count; i++)
        {
            var directive = directives[i];

            if (directive.Name.Value.Equals(isInternal, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetDeferLabel(InlineFragmentNode node)
    {
        for (var i = 0; i < node.Directives.Count; i++)
        {
            var directive = node.Directives[i];

            if (!directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
            {
                continue;
            }

            for (var j = 0; j < directive.Arguments.Count; j++)
            {
                var arg = directive.Arguments[j];

                if (arg.Name.Value.Equals(DirectiveNames.Defer.Arguments.Label, StringComparison.Ordinal)
                    && arg.Value is StringValueNode labelValue)
                {
                    return labelValue.Value;
                }
            }

            return null;
        }

        return null;
    }

    private class IncludeConditionVisitor : SyntaxWalker<IncludeConditionCollection>
    {
        public static readonly IncludeConditionVisitor Instance = new();

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IncludeConditionCollection context)
        {
            if (IncludeCondition.TryCreate(node, out var condition))
            {
                context.Add(condition);
            }

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            IncludeConditionCollection context)
        {
            if (IncludeCondition.TryCreate(node, out var condition))
            {
                context.Add(condition);
            }

            return base.Enter(node, context);
        }
    }

    private class DeferConditionVisitor : SyntaxWalker<DeferConditionCollection>
    {
        public static readonly DeferConditionVisitor Instance = new();

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            DeferConditionCollection context)
        {
            if (DeferCondition.TryCreate(node, out var condition))
            {
                context.Add(condition);
            }

            return base.Enter(node, context);
        }
    }

    private class CompilationContext
    {
        private object[] _elementsById;

        public CompilationContext(object[] elementsById, OperationFeatureCollection? features = null)
        {
            _elementsById = elementsById;
            Features = features ?? new OperationFeatureCollection();
        }

        public object[] ElementsById => _elementsById;

        public OperationFeatureCollection Features { get; }

        public void Register(object element, int id)
        {
            if (id >= _elementsById.Length)
            {
                var newArray = s_objectArrayPool.Rent(_elementsById.Length * 2);
                _elementsById.AsSpan().CopyTo(newArray);
                s_objectArrayPool.Return(_elementsById);
                _elementsById = newArray;
            }

            _elementsById[id] = element;
        }
    }
}
