using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using static System.StringComparison;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// The target field of a field selection must be defined on the scoped
/// type of the selection set. There are no limitations on alias names.
///
/// http://spec.graphql.org/June2018/#sec-Field-Selections-on-Objects-Interfaces-and-Unions-Types
///
/// AND
///
/// Field selections on scalars or enums are never allowed,
/// because they are the leaf nodes of any GraphQL query.
///
/// Conversely the leaf field selections of GraphQL queries
/// must be of type scalar or enum. Leaf selections on objects,
/// interfaces, and unions without subfields are disallowed.
///
/// http://spec.graphql.org/June2018/#sec-Leaf-Field-Selections
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
            foreach (var selectionSet in context.FieldSets.Keys)
            {
                TryMergeFieldsInSet(context, context.FieldSets[selectionSet]);
            }
        }

        if (node.SelectionSet.Selections.Count == 0)
        {
            context.ReportError(context.NoSelectionOnRootType(
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
            fields.Add(new FieldInfo(context.Types.Peek(), context.NonNullString, node));
            return Skip;
        }

        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is IComplexOutputType ct)
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
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is { Kind: TypeKind.Union } unionType &&
            HasFields(node))
        {
            context.ReportError(context.UnionFieldError(node, (UnionType)unionType));
            return Skip;
        }

        if (context.Path.TryPeek(out var parent))
        {
            if (parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
            {
                context.SelectionSets.Push(node);
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(
        SelectionSetNode node,
        IDocumentValidatorContext context)
    {
        if (context.Path.TryPeek(out var parent))
        {
            if (parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
            {
                context.SelectionSets.Pop();
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        if (context.Fragments.TryGetValue(
            node.Name.Value,
            out var fragment) &&
            context.VisitedFragments.Add(fragment.Name.Value))
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
        if (fields.Count == 1)
        {
            if (fields[0].Field.SelectionSet is { } selectionSet &&
                context.FieldSets.TryGetValue(selectionSet, out var fieldSet))
            {
                TryMergeFieldsInSet(context, fieldSet);
            }
        }
        else
        {
            for (var i = 0; i < fields.Count - 1; i++)
            {
                var fieldA = fields[i];
                for (var j = i + 1; j < fields.Count; j++)
                {
                    var fieldB = fields[j];
                    if (!ReferenceEquals(fieldA.Field, fieldB.Field) &&
                        string.Equals(fieldA.ResponseName, fieldB.ResponseName, Ordinal))
                    {
                        if (SameResponseShape(
                            fieldA.Type.RewriteNullability(fieldA.Field.Required),
                            fieldB.Type.RewriteNullability(fieldB.Field.Required)))
                        {
                            if (IsParentTypeAligned(fieldA, fieldB))
                            {
                                if (BySyntax.Equals(fieldA.Field.Name, fieldB.Field.Name) &&
                                    AreArgumentsIdentical(fieldA.Field, fieldB.Field))
                                {
                                    TryMergeFieldsInSet(context, fieldA, fieldB);
                                }
                                else if (context.FieldTuples.Add((fieldA.Field, fieldB.Field)))
                                {
                                    context.ReportError(
                                        context.FieldsAreNotMergeable(fieldA, fieldB));
                                }
                            }
                        }
                        else if (context.FieldTuples.Add((fieldA.Field, fieldB.Field)))
                        {
                            context.ReportError(context.FieldsAreNotMergeable(fieldA, fieldB));
                        }
                    }
                }
            }
        }
    }

    private static void TryMergeFieldsInSet(
        IDocumentValidatorContext context,
        FieldInfo fieldA,
        FieldInfo fieldB)
    {
        if (fieldA.Field.SelectionSet is { } a &&
            fieldB.Field.SelectionSet is { } b &&
            context.FieldSets.TryGetValue(a, out var al) &&
            context.FieldSets.TryGetValue(b, out var bl))
        {
            var mergedSet = context.RentFieldInfoList();
            CopyFieldInfos(al, mergedSet);
            CopyFieldInfos(bl, mergedSet);
            TryMergeFieldsInSet(context, mergedSet);
        }
    }

    private static void CopyFieldInfos(IList<FieldInfo> from, IList<FieldInfo> to)
    {
        for (var i = 0; i < from.Count; i++)
        {
            to.Add(from[i]);
        }
    }

    private static bool IsParentTypeAligned(FieldInfo fieldA, FieldInfo fieldB)
    {
        return ReferenceEquals(fieldA.DeclaringType, fieldB.DeclaringType) ||
            !fieldA.DeclaringType.IsObjectType() && !fieldB.DeclaringType.IsObjectType();
    }

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
            if (typeA.IsNonNullType() || typeB.IsNonNullType())
            {
                if (typeA.IsNullableType() || typeB.IsNullableType())
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

        if (typeA.IsCompositeType() && typeB.IsCompositeType())
        {
            return true;
        }

        return false;
    }
}
