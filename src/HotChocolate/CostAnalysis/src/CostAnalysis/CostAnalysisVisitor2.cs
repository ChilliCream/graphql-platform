using System.Runtime.CompilerServices;
using HotChocolate.CostAnalysis.Directives;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalysisVisitor2 : TypeDocumentValidatorVisitor
{
    private readonly Dictionary<SelectionSetNode, CostSummary> _selectionSetCost = new();
    private readonly HashSet<string> _processed = new();

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
                        return Skip;
                    }
                }
                else
                {
                    if (of.Type.NamedType().IsLeafType())
                    {
                        return Skip;
                    }
                }

                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }

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
            type.NamedType() is { Kind: TypeKind.Union, } &&
            HasFields(node))
        {
            return Skip;
        }

        if (context.Path.TryPeek(out var parent) &&
            parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
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

            if (!context.FieldSets.TryGetValue(node, out var fields))
            {
                return Continue;
            }

            if (!_selectionSetCost.TryGetValue(node, out var costSummary))
            {
                costSummary = new CostSummary();
                _selectionSetCost.Add(node, costSummary);
            }

            var type = context.Types.Peek().NamedType();

            if (type is ObjectType objectType)
            {
                costSummary.FieldCost = CalculateSelectionSetCost(objectType, fields, _processed, GetSelectionSetCost);
                _processed.Clear();
            }
            else
            {
                var max = 0.0;

                foreach (var possibleType in context.Schema.GetPossibleTypes(type))
                {
                    var cost = CalculateSelectionSetCost(possibleType, fields, _processed, GetSelectionSetCost);

                    if (max < cost)
                    {
                        max = cost;
                    }

                    _processed.Clear();
                }

                costSummary.FieldCost = max;
            }
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction VisitChildren(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        if (context.Fragments.TryGetValue(node.Name.Value, out var fragment) &&
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
                if (!IsTypeNameField(((FieldNode) selection).Name.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private CostSummary GetSelectionSetCost(SelectionSetNode selectionSetNode)
        => _selectionSetCost[selectionSetNode];

    private static double CalculateSelectionSetCost(
        ObjectType possibleType,
        IList<FieldInfo> fields,
        HashSet<string> processed,
        Func<SelectionSetNode, CostSummary> getSelectionSetCost)
    {
        var cost = 0.0;

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];

            if (!processed.Add(field.ResponseName) &&
                field.DeclaringType.NamedType().IsAssignableFrom(possibleType) &&
                possibleType.Fields.TryGetField(field.Field.Name.Value, out var objectField))
            {
                var fieldCost = objectField.GetCostWeight();

                if (objectField.Type.IsCompositeType() && field.Field.SelectionSet is not null)
                {
                    fieldCost += getSelectionSetCost(field.Field.SelectionSet).FieldCost;
                }

                if (objectField.Type.IsListType())
                {
                    fieldCost *= objectField.GetListSize(field.Field);
                }

                cost += fieldCost;
            }
        }

        return cost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTypeNameField(string fieldName)
        => fieldName.EqualsOrdinal(IntrospectionFields.TypeName);

    private sealed class CostSummary
    {
        public double TypeCost { get; set; }
        public double FieldCost { get; set; }
    }
}

file static class Helpers
{
    public static double GetCostWeight(this IOutputField field)
    {
        // Use weight from @cost directive.
        if (field is IHasDirectives fieldWithDirectives)
        {
            var costDirective =
                fieldWithDirectives.Directives
                    .FirstOrDefault<CostDirective>()?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return double.Parse(costDirective.Weight);
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return field.Type.IsCompositeType() || field.Type.IsListType() ? 1.0 : 0.0;
    }

    public static double GetListSize(this IOutputField field, FieldNode node)
    {
        return 1.0;
    }
}
