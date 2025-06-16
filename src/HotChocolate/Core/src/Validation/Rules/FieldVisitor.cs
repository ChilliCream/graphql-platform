using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// The target field of a field selection must be defined on the scoped
/// type of the selection set. There are no limitations on alias names.
///
/// https://spec.graphql.org/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
///
/// AND
///
/// Field selections on scalars or enums are never allowed,
/// because they are the leaf nodes of any GraphQL query.
///
/// Conversely, the leaf field selections of GraphQL queries
/// must be of type scalar or enum. Leaf selections on objects,
/// interfaces, and unions without subfields are disallowed.
///
/// https://spec.graphql.org/June2018/#sec-Leaf-Field-Selections
/// </summary>
internal sealed class FieldVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        // The document node is the root node entered once per visitation.
        // We use this hook to ensure that the field visitor feature is created,
        // and we can use it in consecutive visits of child nodes without extra
        // checks at each point.
        // We do use a GetOrSet here because the context is a pooled object.
        context.Features.GetOrSet<FieldVisitorFeature>();
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        if (!context.Schema.TryGetOperationType(node.Operation, out _))
        {
            context.ReportError(context.OperationNotSupported(node.Operation));
            return Skip;
        }

        var feature = context.Features.GetRequired<FieldVisitorFeature>();
        feature.FieldSets.Clear();
        context.SelectionSets.Clear();

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();

        if (feature.FieldSets.Count > 0)
        {
            foreach (var item in feature.FieldSets)
            {
                TryMergeFieldsInSet(context, item.Value);
            }

            var next = feature.NextFieldPairs;

            while (next.Count > 0)
            {
                FillCurrentFieldPairs(context);
                ProcessCurrentFieldPairs(context);
            }
        }

        if (node.SelectionSet.Selections.Count == 0)
        {
            context.ReportError(
                context.NoSelectionOnRootType(
                    node,
                    context.Schema.GetOperationType(node.Operation)));
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();
        var selectionSet = context.SelectionSets.Peek();

        if (!feature.FieldSets.TryGetValue(selectionSet, out var fields))
        {
            fields = feature.RentFieldInfoList();
            feature.FieldSets.Add(selectionSet, fields);
        }

        if (IntrospectionFieldNames.TypeName.Equals(node.Name.Value, StringComparison.Ordinal))
        {
            if (node.IsStreamable())
            {
                context.ReportError(context.StreamOnNonListField(node));
            }

            fields.Add(new FieldInfo(context.Types.Peek(), feature.NonNullString, node));
            return Skip;
        }

        if (context.Types.TryPeek(out var type) && type.NamedType() is IComplexTypeDefinition ct)
        {
            if (ct.Fields.TryGetField(node.Name.Value, out var of))
            {
                fields.Add(new FieldInfo(context.Types.Peek(), of.Type, node));

                if (node.SelectionSet is null || node.SelectionSet.Selections.Count == 0)
                {
                    if (of.Type.NamedType().IsCompositeType())
                    {
                        context.ReportError(
                            context.NoSelectionOnCompositeField(node, ct, of.Type));
                    }
                }
                else
                {
                    if (of.Type.NamedType().IsLeafType())
                    {
                        context.ReportError(
                            context.LeafFieldsCannotHaveSelections(node, ct, of.Type));
                        return Skip;
                    }
                }

                // if the directive is annotated with the @stream directive then the fields
                // return type must bi a list.
                if (node.IsStreamable() && !of.Type.IsListType())
                {
                    context.ReportError(context.StreamOnNonListField(node));
                }

                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }

            context.ReportError(context.FieldDoesNotExist(node, ct));
            return Skip;
        }

        context.UnexpectedErrorsDetected = true;
        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        FieldNode node,
        DocumentValidatorContext context)
    {
        context.OutputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectionSetNode node,
        DocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is { Kind: TypeKind.Union } unionType
            && HasFields(node))
        {
            context.ReportError(context.UnionFieldError(node, unionType.ExpectUnionType()));
            return Skip;
        }

        if (context.Path.TryPeek(out var parent) && parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
        {
            context.SelectionSets.Push(node);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        SelectionSetNode node,
        DocumentValidatorContext context)
    {
        if (!context.Path.TryPeek(out var parent))
        {
            return Continue;
        }

        if (parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
        {
            context.SelectionSets.Pop();
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        if (context.Fragments.TryEnter(node, out var fragment))
        {
            var result = Visit(fragment, node, context);
            context.Fragments.Leave(node);

            if (result.IsBreak())
            {
                return Break;
            }
        }

        return DefaultAction;
    }

    private static bool HasFields(SelectionSetNode selectionSet)
    {
        for (var i = 0; i < selectionSet.Selections.Count; i++)
        {
            var selection = selectionSet.Selections[i];

            if (selection.Kind is SyntaxKind.Field)
            {
                if (!IsTypeNameField(((FieldNode)selection).Name.Value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeNameField(string fieldName)
        => fieldName.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal);

    private static void TryMergeFieldsInSet(
        DocumentValidatorContext context,
        IList<FieldInfo> fields)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();

        while (true)
        {
            if (fields.Count == 1)
            {
                if (fields[0].SyntaxNode.SelectionSet is { } selectionSet
                    && feature.FieldSets.TryGetValue(selectionSet, out var fieldSet))
                {
                    fields = fieldSet;
                    continue;
                }

                return;
            }

            break;
        }

        for (var i = 0; i < fields.Count - 1; i++)
        {
            var fieldA = fields[i];

            for (var j = i + 1; j < fields.Count; j++)
            {
                var fieldB = fields[j];

                if (ReferenceEquals(fieldA.SyntaxNode, fieldB.SyntaxNode)
                    || !fieldA.ResponseName.Equals(fieldB.ResponseName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (SameResponseShape(fieldA.Type, fieldB.Type) && SameStreamDirective(fieldA, fieldB))
                {
                    if (!IsParentTypeAligned(fieldA, fieldB))
                    {
                        continue;
                    }

                    if (BySyntax.Equals(fieldA.SyntaxNode.Name, fieldB.SyntaxNode.Name)
                        && AreArgumentsIdentical(fieldA.SyntaxNode, fieldB.SyntaxNode))
                    {
                        var pair = new FieldInfoPair(fieldA, fieldB);
                        if (feature.ProcessedFieldPairs.Add(pair))
                        {
                            feature.NextFieldPairs.Add(pair);
                        }
                    }
                    else if (feature.FieldTuples.Add((fieldA.SyntaxNode, fieldB.SyntaxNode)))
                    {
                        context.ReportError(context.FieldsAreNotMergeable(fieldA, fieldB));
                    }
                }
                else if (feature.FieldTuples.Add((fieldA.SyntaxNode, fieldB.SyntaxNode)))
                {
                    context.ReportError(context.FieldsAreNotMergeable(fieldA, fieldB));
                }
            }
        }
    }

    private static void TryMergeFieldsInSet(
        DocumentValidatorContext context,
        FieldInfo fieldA,
        FieldInfo fieldB)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();

        if (fieldA.SyntaxNode.SelectionSet is { } a
            && fieldB.SyntaxNode.SelectionSet is { } b
            && feature.FieldSets.TryGetValue(a, out var al)
            && feature.FieldSets.TryGetValue(b, out var bl))
        {
            var mergedSet = Unsafe.As<List<FieldInfo>>(feature.RentFieldInfoList());
            mergedSet.EnsureCapacity(al.Count + bl.Count);
            CopyFieldInfos(Unsafe.As<List<FieldInfo>>(al), mergedSet);
            CopyFieldInfos(Unsafe.As<List<FieldInfo>>(bl), mergedSet);
            TryMergeFieldsInSet(context, mergedSet);
        }
    }

    private static void CopyFieldInfos(List<FieldInfo> from, List<FieldInfo> to)
    {
        ref var field = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(from));
        ref var end = ref Unsafe.Add(ref field, from.Count);

        while (Unsafe.IsAddressLessThan(ref field, ref end))
        {
            to.Add(field);
            field = ref Unsafe.Add(ref field, 1);
        }
    }

    private static bool IsParentTypeAligned(FieldInfo fieldA, FieldInfo fieldB)
        => ReferenceEquals(fieldA.DeclaringType, fieldB.DeclaringType)
            || (!fieldA.DeclaringType.IsObjectType() && !fieldB.DeclaringType.IsObjectType());

    private static bool AreArgumentsIdentical(FieldNode fieldA, FieldNode fieldB)
    {
        if (fieldA.Arguments.Count == 0 && fieldB.Arguments.Count == 0)
        {
            return true;
        }

        if (fieldA.Arguments.Count != fieldB.Arguments.Count)
        {
            return false;
        }

        var validPairs = 0;

        for (var i = 0; i < fieldA.Arguments.Count; i++)
        {
            var argumentA = fieldA.Arguments[i];

            for (var j = 0; j < fieldB.Arguments.Count; j++)
            {
                var argumentB = fieldB.Arguments[j];

                if (BySyntax.Equals(argumentA.Name, argumentB.Name))
                {
                    if (BySyntax.Equals(argumentA.Value, argumentB.Value))
                    {
                        validPairs++;
                    }

                    break;
                }
            }
        }

        return fieldA.Arguments.Count == validPairs;
    }

    private static bool SameResponseShape(IType typeA, IType typeB)
    {
        while (!typeA.IsNamedType() && !typeB.IsNamedType())
        {
            if (typeA.Kind is TypeKind.NonNull || typeB.Kind is TypeKind.NonNull)
            {
                if (typeA.Kind is not TypeKind.NonNull || typeB.Kind is not TypeKind.NonNull)
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();
            }

            if (typeA.IsListType() || typeB.IsListType())
            {
                if (!typeA.IsListType() || !typeB.IsListType())
                {
                    return false;
                }

                typeA = typeA.ElementType();
                typeB = typeB.ElementType();
            }
        }

        if (typeA.IsLeafType() || typeB.IsLeafType())
        {
            return ReferenceEquals(typeA, typeB);
        }

        return typeA.IsCompositeType() && typeB.IsCompositeType();
    }

    private static bool SameStreamDirective(FieldInfo fieldA, FieldInfo fieldB)
    {
        var streamA = fieldA.SyntaxNode.GetStreamDirective();
        var streamB = fieldB.SyntaxNode.GetStreamDirective();

        // if both fields do not have any stream directive they are mergeable.
        if (streamA is null)
        {
            return streamB is null;
        }

        // if stream A is not nullable and stream b is null then we cannot merge.
        if (streamB is null)
        {
            return false;
        }

        // if both fields have a stream directive we need to check if they are equal.
        return streamA.StreamDirectiveEquals(streamB);
    }

    private static void FillCurrentFieldPairs(DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();
        var next = feature.NextFieldPairs;
        var current = feature.CurrentFieldPairs;

        ref var pair = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(next));
        ref var end = ref Unsafe.Add(ref pair, next.Count);

        current.EnsureCapacity(next.Count);

        while (Unsafe.IsAddressLessThan(ref pair, ref end))
        {
            current.Add(pair);
            pair = ref Unsafe.Add(ref pair, 1);
        }

        next.Clear();
    }

    private static void ProcessCurrentFieldPairs(DocumentValidatorContext context)
    {
        var feature = context.Features.GetRequired<FieldVisitorFeature>();
        var current = feature.CurrentFieldPairs;

        ref var pair = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(current));
        ref var end = ref Unsafe.Add(ref pair, current.Count);

        while (Unsafe.IsAddressLessThan(ref pair, ref end))
        {
            TryMergeFieldsInSet(context, pair.FieldA, pair.FieldB);
            pair = ref Unsafe.Add(ref pair, 1);
        }

        current.Clear();
    }

    private sealed class FieldVisitorFeature : ValidatorFeature
    {
        private static readonly FieldInfoListBufferPool s_fieldInfoPool = new();
        private readonly List<FieldInfoListBuffer> _buffers = [new()];

        public IType NonNullString { get; private set; } = null!;

        public List<FieldInfoPair> CurrentFieldPairs { get; } = [];

        public List<FieldInfoPair> NextFieldPairs { get; } = [];

        public HashSet<FieldInfoPair> ProcessedFieldPairs { get; } = [];

        public FieldDepthCycleTracker FieldDepth { get; } = new();

        public Dictionary<SelectionSetNode, IList<FieldInfo>> FieldSets { get; } = [];

        public HashSet<(FieldNode, FieldNode)> FieldTuples { get; } = [];

        public IList<FieldInfo> RentFieldInfoList()
        {
            var buffer = _buffers.Peek();
            List<FieldInfo>? list;

            while (!buffer.TryPop(out list))
            {
                buffer = s_fieldInfoPool.Get();
                _buffers.Push(buffer);
            }

            return list;
        }

        protected internal override void Initialize(DocumentValidatorContext context)
            => NonNullString = new NonNullType(context.Schema.Types.GetType<IScalarTypeDefinition>("String"));

        protected internal override void Reset()
        {
            NonNullString = null!;
            CurrentFieldPairs.Clear();
            NextFieldPairs.Clear();
            ProcessedFieldPairs.Clear();
            FieldDepth.Reset();
            FieldSets.Clear();
            FieldTuples.Clear();

            if (_buffers.Count > 1)
            {
                var buffer = _buffers.Pop();
                buffer.Clear();

                for (var i = 0; i < _buffers.Count; i++)
                {
                    s_fieldInfoPool.Return(_buffers[i]);
                }

                _buffers.Push(buffer);
            }
            else
            {
                _buffers[0].Clear();
            }
        }
    }
}

file static class DirectiveExtensions
{
    public static bool StreamDirectiveEquals(
        this DirectiveNode streamA,
        DirectiveNode streamB)
    {
        var argsA = CreateStreamArgs(streamA);
        var argsB = CreateStreamArgs(streamB);

        return BySyntax.Equals(argsA.If, argsB.If)
            && BySyntax.Equals(argsA.InitialCount, argsB.InitialCount)
            && BySyntax.Equals(argsA.Label, argsB.Label);
    }

    private static StreamArgs CreateStreamArgs(DirectiveNode directiveNode)
    {
        var args = new StreamArgs();

        for (var i = 0; i < directiveNode.Arguments.Count; i++)
        {
            var argument = directiveNode.Arguments[i];

            switch (argument.Name.Value)
            {
                case DirectiveNames.Stream.Arguments.If:
                    args.If = argument.Value;
                    break;

                case DirectiveNames.Stream.Arguments.Label:
                    args.Label = argument.Value;
                    break;

                case DirectiveNames.Stream.Arguments.InitialCount:
                    args.InitialCount = argument.Value;
                    break;
            }
        }

        return args;
    }

    private ref struct StreamArgs
    {
        public IValueNode? If { get; set; }

        public IValueNode? Label { get; set; }

        public IValueNode? InitialCount { get; set; }
    }
}
