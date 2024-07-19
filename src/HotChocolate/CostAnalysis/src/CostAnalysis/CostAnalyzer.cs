using System.Runtime.CompilerServices;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzer(CostOptions options) : TypeDocumentValidatorVisitor
{
    private readonly Dictionary<SelectionSetNode, CostSummary> _selectionSetCost = new();
    private readonly HashSet<string> _processed = new();
    private readonly InputCostVisitor _inputCostVisitor = new();
    private InputCostVisitorContext? _inputCostVisitorContext;

    public CostMetrics Analyze(OperationDefinitionNode operation, IDocumentValidatorContext context)
    {
        Visit(operation, context);

        var summary = _selectionSetCost[operation.SelectionSet];

        _selectionSetCost.Clear();
        _processed.Clear();
        _inputCostVisitorContext = null;

        return new CostMetrics { TypeCost = summary.TypeCost, FieldCost = summary.FieldCost };
    }

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
        // add operation cost
        var type = context.Types.Peek();
        var cost = type.GetTypeWeight();
        var summary = _selectionSetCost[node.SelectionSet];
        summary.TypeCost += cost;

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

        if (context.Types.TryPeek(out var type) && type.NamedType() is IComplexOutputType ct)
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
        if (context.Types.TryPeek(out var type)
            && type.NamedType() is { Kind: TypeKind.Union }
            && HasFields(node))
        {
            return Skip;
        }

        if (context.Path.TryPeek(out var parent)
            && parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
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
                var result = CalculateSelectionSetCost(
                    objectType,
                    fields,
                    context);

                costSummary.TypeCost = result.TypeCost;
                costSummary.FieldCost = result.FieldCost;
            }
            else
            {
                foreach (var possibleType in context.Schema.GetPossibleTypes(type))
                {
                    var result = CalculateSelectionSetCost(
                        possibleType,
                        fields,
                        context);

                    if (costSummary.TypeCost < result.TypeCost)
                    {
                        costSummary.TypeCost = result.TypeCost;
                    }

                    if (costSummary.FieldCost < result.FieldCost)
                    {
                        costSummary.FieldCost = result.FieldCost;
                    }
                }
            }
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

    private CostSummary GetSelectionSetCost(SelectionSetNode selectionSetNode)
        => _selectionSetCost[selectionSetNode];

    private (double TypeCost, double FieldCost) CalculateSelectionSetCost(
        ObjectType possibleType,
        IList<FieldInfo> fields,
        IDocumentValidatorContext context)
    {
        _processed.Clear();

        var typeCostSum = 0.0;
        var fieldCostSum = 0.0;

        for (var i = 0; i < fields.Count; i++)
        {
            var fieldInfo = fields[i];
            var fieldNode = fieldInfo.SyntaxNode;

            if (_processed.Add(fieldInfo.ResponseName)
                && fieldInfo.DeclaringType.NamedType().IsAssignableFrom(possibleType)
                && possibleType.Fields.TryGetField(fieldNode.Name.Value, out var field))
            {
                // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost
                // First, add up the raw cost of field f by calculating the sum of:
                // - The weight of field f
                // - The sum of the cost of all directives on field f
                // - The sum of the cost of all arguments on field f
                var typeCost = field.GetTypeWeight();
                var fieldCost = field.GetFieldWeight();
                var selectionSetCost = 0.0;
                var listSizeDirective = field.Directives
                    .FirstOrDefault<ListSizeDirective>()
                    ?.AsValue<ListSizeDirective>();

                listSizeDirective.ValidateRequireOneSlicingArgument(fieldNode, context.Path);

                if (fieldNode.Arguments.Count > 0)
                {
                    foreach (var argumentNode in fieldNode.Arguments)
                    {
                        if (field.Arguments.TryGetField(argumentNode.Name.Value, out var argument))
                        {
                            var argumentCost = argument.GetFieldWeight();
                            _inputCostVisitorContext ??= new InputCostVisitorContext();
                            _inputCostVisitor.CalculateCost(argument, argumentNode, _inputCostVisitorContext);
                            argumentCost += _inputCostVisitorContext.Cost;

                            if ((argument.Flags & FieldFlags.FilterArgument) == FieldFlags.FilterArgument
                                && argumentNode.Value.Kind == SyntaxKind.Variable
                                && options.Filtering.VariableMultiplier.HasValue)
                            {
                                argumentCost *= options.Filtering.VariableMultiplier.Value;
                            }

                            fieldCost += argumentCost;
                        }
                    }
                }

                if (field.Type.NamedType().IsCompositeType() && fieldNode.SelectionSet is not null)
                {
                    var selectionSetCostSummary = GetSelectionSetCost(fieldNode.SelectionSet);
                    selectionSetCost += selectionSetCostSummary.FieldCost;
                    typeCost += selectionSetCostSummary.TypeCost;
                }

                if (field.Type.IsListType())
                {
                    var arguments = fieldNode.Arguments;
                    var parentField = context.OutputFields.PeekOrDefault();

                    if (listSizeDirective is null && parentField is not null)
                    {
                        var parentListSizeDirective = parentField.Directives
                            .FirstOrDefault<ListSizeDirective>()
                            ?.AsValue<ListSizeDirective>();

                        if (parentListSizeDirective?.SizedFields.Contains(field.Name) ?? false)
                        {
                            listSizeDirective = parentListSizeDirective;
                            arguments = context.Path.PeekOrDefault<ISyntaxNode, FieldNode>()!.Arguments;
                        }
                    }

                    // if the field is a list type we are multiplying the cost
                    // by the estimated list size.
                    var listSize = field.GetListSize(arguments, listSizeDirective, context.Variables);
                    typeCost *= listSize;
                    selectionSetCost *= listSize;
                }

                fieldCost += selectionSetCost;

                // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost
                // https://ibm.github.io/graphql-specs/cost-spec.html#sel-IALJJHPDABAB4Biqc
                // Second, if this sum is negative then round it up to zero
                if(fieldCost < 0)
                {
                    fieldCost = 0;
                }

                // last we sum up the field cost and the type cost for the selection-set.
                typeCostSum += typeCost;
                fieldCostSum += fieldCost;
            }
        }

        return (typeCostSum, fieldCostSum);
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
