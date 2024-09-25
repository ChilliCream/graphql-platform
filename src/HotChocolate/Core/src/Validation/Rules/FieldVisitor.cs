using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
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
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        context.FieldSets.Clear();
        context.SelectionSets.Clear();

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        if (context.FieldSets.Count > 0)
        {
            foreach (var item in context.FieldSets)
            {
                TryMergeFieldsInSet(context, item.Value);
            }

            var next = context.NextFieldPairs;

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
                    context.Schema.GetOperationType(node.Operation)!));
        }

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        var selectionSet = context.SelectionSets.Peek();

        if (!context.FieldSets.TryGetValue(selectionSet, out var fields))
        {
            fields = context.RentFieldInfoList();
            context.FieldSets.Add(selectionSet, fields);
        }

        if (IntrospectionFields.TypeName.EqualsOrdinal(node.Name.Value))
        {
            if (node.IsStreamable())
            {
                context.ReportError(context.StreamOnNonListField(node));
            }

            fields.Add(new FieldInfo(context.Types.Peek(), context.NonNullString, node));
            return Skip;
        }

        if (context.Types.TryPeek(out var type) && type.NamedType() is IComplexOutputType ct)
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
        IDocumentValidatorContext context)
    {
        context.OutputFields.Pop();
        context.Types.Pop();
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        SelectionSetNode node,
        IDocumentValidatorContext context)
    {
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is { Kind: TypeKind.Union, } unionType
            && HasFields(node))
        {
            context.ReportError(context.UnionFieldError(node, (UnionType)unionType));
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
        IDocumentValidatorContext context)
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
        IDocumentValidatorContext context)
    {
        if (context.Fragments.TryGetValue(node.Name.Value, out var fragment)
            && context.VisitedFragments.Add(fragment.Name.Value))
        {
            var result = Visit(fragment, node, context);
            context.VisitedFragments.Remove(fragment.Name.Value);

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
        => fieldName.EqualsOrdinal(IntrospectionFields.TypeName);

    private static void TryMergeFieldsInSet(
        IDocumentValidatorContext context,
        IList<FieldInfo> fields)
    {
        while (true)
        {
            if (fields.Count == 1)
            {
                if (fields[0].SyntaxNode.SelectionSet is { } selectionSet
                    && context.FieldSets.TryGetValue(selectionSet, out var fieldSet))
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
                    || !fieldA.ResponseName.EqualsOrdinal(fieldB.ResponseName))
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
                        if (context.ProcessedFieldPairs.Add(pair))
                        {
                            context.NextFieldPairs.Add(pair);
                        }
                    }
                    else if (context.FieldTuples.Add((fieldA.SyntaxNode, fieldB.SyntaxNode)))
                    {
                        context.ReportError(context.FieldsAreNotMergeable(fieldA, fieldB));
                    }
                }
                else if (context.FieldTuples.Add((fieldA.SyntaxNode, fieldB.SyntaxNode)))
                {
                    context.ReportError(context.FieldsAreNotMergeable(fieldA, fieldB));
                }
            }
        }
    }

    private static void TryMergeFieldsInSet(
        IDocumentValidatorContext context,
        FieldInfo fieldA,
        FieldInfo fieldB)
    {
        if (fieldA.SyntaxNode.SelectionSet is { } a
            && fieldB.SyntaxNode.SelectionSet is { } b
            && context.FieldSets.TryGetValue(a, out var al)
            && context.FieldSets.TryGetValue(b, out var bl))
        {
            var mergedSet = Unsafe.As<List<FieldInfo>>(context.RentFieldInfoList());
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

            if (typeA.IsType(TypeKind.List) || typeB.IsType(TypeKind.List))
            {
                if (!typeA.IsType(TypeKind.List) || !typeB.IsType(TypeKind.List))
                {
                    return false;
                }

                typeA = typeA.ElementType();
                typeB = typeB.ElementType();
            }
        }

        if (typeA.IsType(TypeKind.Scalar, TypeKind.Enum) || typeB.IsType(TypeKind.Scalar, TypeKind.Enum))
        {
            return ReferenceEquals(typeA, typeB);
        }

        if (typeA.IsType(TypeKind.Object, TypeKind.Interface, TypeKind.Union)
            && typeB.IsType(TypeKind.Object, TypeKind.Interface, TypeKind.Union))
        {
            return true;
        }

        return false;
    }

    private static bool SameStreamDirective(FieldInfo fieldA, FieldInfo fieldB)
    {
        var streamA = fieldA.SyntaxNode.GetStreamDirectiveNode();
        var streamB = fieldB.SyntaxNode.GetStreamDirectiveNode();

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

    private static void FillCurrentFieldPairs(IDocumentValidatorContext context)
    {
        var next = context.NextFieldPairs;
        var current = context.CurrentFieldPairs;

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

    private static void ProcessCurrentFieldPairs(IDocumentValidatorContext context)
    {
        var current = context.CurrentFieldPairs;

        ref var pair = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(current));
        ref var end = ref Unsafe.Add(ref pair, current.Count);

        while (Unsafe.IsAddressLessThan(ref pair, ref end))
        {
            TryMergeFieldsInSet(context, pair.FieldA, pair.FieldB);
            pair = ref Unsafe.Add(ref pair, 1);
        }

        current.Clear();
    }
}
