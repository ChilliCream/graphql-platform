using System.Buffers;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class OperationCompiler
{
    private readonly FusionSchemaDefinition _schema;
    private readonly DocumentRewriter _documentRewriter;
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldsPool;
    private readonly TypeNameField _typeNameField;
    private static readonly ArrayPool<object> s_objectArrayPool = ArrayPool<object>.Shared;

    public OperationCompiler(
        FusionSchemaDefinition schema,
        ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> fieldsPool)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(fieldsPool);

        _schema = schema;
        _fieldsPool = fieldsPool;
        _documentRewriter = new(schema, removeStaticallyExcludedSelections: true);
        var nonNullStringType = new NonNullType(_schema.Types.GetType<IScalarTypeDefinition>(SpecScalarNames.String.Name));
        _typeNameField = new TypeNameField(nonNullStringType);
    }

    public Operation Compile(string id, string hash, OperationDefinitionNode operationDefinition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(operationDefinition);

        var document = new DocumentNode(new IDefinitionNode[] { operationDefinition });
        document = _documentRewriter.RewriteDocument(document);
        operationDefinition = (OperationDefinitionNode)document.Definitions[0];

        var includeConditions = new IncludeConditionCollection();
        IncludeConditionVisitor.Instance.Visit(operationDefinition, includeConditions);
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
                includeConditions);

            var selectionSet = BuildSelectionSet(
                fields,
                rootType,
                compilationContext,
                ref lastId);

            compilationContext.Register(selectionSet, selectionSet.Id);

            return new Operation(
                id,
                hash,
                operationDefinition,
                rootType,
                _schema,
                selectionSet,
                this,
                includeConditions,
                lastId,
                compilationContext.ElementsById); // Pass the populated array
        }
        finally
        {
            _fieldsPool.Return(fields);
        }
    }

    internal SelectionSet CompileSelectionSet(
        Selection selection,
        FusionObjectTypeDefinition objectType,
        IncludeConditionCollection includeConditions,
        ref object[] elementsById,
        ref int lastId)
    {
        var compilationContext = new CompilationContext(elementsById);
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
                includeConditions);

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
                        includeConditions);
                }
            }

            var selectionSet = BuildSelectionSet(fields, objectType, compilationContext, ref lastId);
            compilationContext.Register(selectionSet, selectionSet.Id);
            elementsById = compilationContext.ElementsById;
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
        IncludeConditionCollection includeConditions)
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

                nodes.Add(new FieldSelectionNode(fieldNode, pathIncludeFlags));
            }

            if (selection is InlineFragmentNode inlineFragmentNode
                && DoesTypeApply(inlineFragmentNode.TypeCondition, typeContext))
            {
                var pathIncludeFlags = parentIncludeFlags;

                if (IncludeCondition.TryCreate(inlineFragmentNode, out var includeCondition))
                {
                    var index = includeConditions.IndexOf(includeCondition);
                    pathIncludeFlags |= 1ul << index;
                }

                CollectFields(
                    pathIncludeFlags,
                    inlineFragmentNode.SelectionSet.Selections,
                    typeContext,
                    fields,
                    includeConditions);
            }
        }
    }

    private SelectionSet BuildSelectionSet(
        OrderedDictionary<string, List<FieldSelectionNode>> fieldMap,
        FusionObjectTypeDefinition typeContext,
        CompilationContext compilationContext,
        ref int lastId)
    {
        var i = 0;
        var selections = new Selection[fieldMap.Count];
        var isConditional = false;
        var includeFlags = new List<ulong>();
        var selectionSetId = ++lastId;

        foreach (var (responseName, nodes) in fieldMap)
        {
            includeFlags.Clear();

            var first = nodes[0];
            var isInternal = IsInternal(first.Node);

            if (first.PathIncludeFlags > 0)
            {
                includeFlags.Add(first.PathIncludeFlags);
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

                    if (next.PathIncludeFlags > 0)
                    {
                        includeFlags.Add(next.PathIncludeFlags);
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

            IOutputFieldDefinition field = first.Node.Name.Value.Equals(IntrospectionFieldNames.TypeName)
                ? _typeNameField
                : typeContext.Fields.GetField(first.Node.Name.Value, allowInaccessibleFields: true);

            var selection = new Selection(
                ++lastId,
                responseName,
                field,
                nodes.ToArray(),
                includeFlags.ToArray(),
                isInternal);

            // Register the selection in the elements array
            compilationContext.Register(selection, selection.Id);
            selections[i++] = selection;

            if (includeFlags.Count > 0)
            {
                isConditional = true;
            }
        }

        return new SelectionSet(selectionSetId, typeContext, selections, isConditional);
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

    private class CompilationContext(object[] elementsById)
    {
        private object[] _elementsById = elementsById;

        public object[] ElementsById => _elementsById;

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
