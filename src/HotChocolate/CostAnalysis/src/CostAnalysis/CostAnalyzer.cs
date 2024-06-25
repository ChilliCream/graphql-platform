using System.Runtime.CompilerServices;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using HotChocolate.Validation;
using IHasDirectives = HotChocolate.Types.IHasDirectives;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzer : TypeDocumentValidatorVisitor
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

        return new CostMetrics
        {
            TypeCost = summary.TypeCost,
            FieldCost = summary.FieldCost
        };
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
            type.NamedType() is { Kind: TypeKind.Union } &&
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

            if (_processed.Add(fieldInfo.ResponseName) &&
                fieldInfo.DeclaringType.NamedType().IsAssignableFrom(possibleType) &&
                possibleType.Fields.TryGetField(fieldNode.Name.Value, out var field))
            {
                var typeCost = field.GetTypeWeight();
                var fieldCost = field.GetFieldWeight();
                var selectionSetCost = 0.0;
                var listSizeDirective = field.Directives
                    .FirstOrDefault<ListSizeDirective>()
                    ?.AsValue<ListSizeDirective>();

                listSizeDirective.ValidateRequireOneSlicingArgument(fieldNode);

                if (fieldNode.Arguments.Count > 0)
                {
                    foreach (var argumentNode in fieldNode.Arguments)
                    {
                        if (field.Arguments.TryGetField(argumentNode.Name.Value, out var argument))
                        {
                            fieldCost += argument.GetFieldWeight();
                            _inputCostVisitorContext ??= new InputCostVisitorContext();
                            _inputCostVisitor.CalculateCost(argument, argumentNode, _inputCostVisitorContext);
                            fieldCost += _inputCostVisitorContext.Cost;
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

// todo ... we should have them as proper extension methods
file static class Helpers
{
    public static double GetFieldWeight(this IOutputField field)
    {
        // Use weight from @cost directive.
        var costDirective = field.Directives
            .FirstOrDefault<CostDirective>()
            ?.AsValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Fields returning scalar and enum types, arguments of scalar and enum types,
        // as well as input fields of scalar and enum types all default to "0.0"."
        return field.Type.NamedType().IsCompositeType() || field.Type.IsListType() ? 1.0 : 0.0;
    }

    public static double GetFieldWeight(this IInputField field)
    {
        // Use weight from @cost directive.
        var costDirective = field.Directives
            .FirstOrDefault<CostDirective>()
            ?.AsValue<CostDirective>();

        if (costDirective is not null)
        {
            return costDirective.Weight;
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Fields returning scalar and enum types, arguments of scalar and enum types,
        // as well as input fields of scalar and enum types all default to "0.0"."
        return field.Type.NamedType().IsInputObjectType() ? 1.0 : 0.0;
    }

    public static double GetTypeWeight(this IOutputField field)
    {
        var namedType = field.Type.NamedType();

        if (namedType is IHasDirectives directiveProvider)
        {
            var costDirective = directiveProvider.Directives
                .FirstOrDefault<CostDirective>()
                ?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return costDirective.Weight;
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return namedType.IsCompositeType() ? 1.0 : 0.0;
    }

    public static double GetTypeWeight(this IType type)
    {
        var namedType = type.NamedType();

        if (namedType is IHasDirectives directiveProvider)
        {
            var costDirective = directiveProvider.Directives
                .FirstOrDefault<CostDirective>()
                ?.AsValue<CostDirective>();

            if (costDirective is not null)
            {
                return costDirective.Weight;
            }
        }

        // https://ibm.github.io/graphql-specs/cost-spec.html#sec-weight
        // "Weights for all composite input and output types default to "1.0""
        return namedType.IsCompositeType() ? 1.0 : 0.0;
    }

    public static double GetListSize(
        this IOutputField field,
        IReadOnlyList<ArgumentNode> arguments,
        ListSizeDirective? listSizeDirective,
        IDictionary<string, VariableDefinitionNode> variables)
    {
        const int defaultListSize = 1;

        if (listSizeDirective is null)
        {
            return defaultListSize;
        }

        if (listSizeDirective.SlicingArguments.Length > 0)
        {
            var index = 0;
            Span<int> slicingValues = stackalloc int[listSizeDirective.SlicingArguments.Length];
            foreach (var slicingArgumentName in listSizeDirective.SlicingArguments)
            {
                var slicingArgument = arguments.SingleOrDefault(a => a.Name.Value == slicingArgumentName);

                if (slicingArgument is not null)
                {
                    switch (slicingArgument.Value)
                    {
                        case IntValueNode intValueNode:
                            slicingValues[index++] = intValueNode.ToInt32();
                            continue;

                        case VariableNode variableNode
                            when variables[variableNode.Name.Value].DefaultValue is
                                IntValueNode intValueNode:
                            slicingValues[index++] = intValueNode.ToInt32();
                            continue;
                    }
                }

                if (field.Arguments.TryGetField(slicingArgumentName, out var argument)
                    && argument.DefaultValue is IntValueNode defaultValueNode)
                {
                    slicingValues[index++] = defaultValueNode.ToInt32();
                }
            }

            if (index == 1)
            {
                return slicingValues[0];
            }

            if (index > 1)
            {
                var max = 0;

                for (var i = 0; i < index; i++)
                {
                    var value = slicingValues[i];
                    if (value > max)
                    {
                        max = value;
                    }
                }

                return max;
            }
        }

        return listSizeDirective.AssumedSize ?? defaultListSize;
    }

    public static void ValidateRequireOneSlicingArgument(
        this ListSizeDirective? listSizeDirective,
        FieldNode node)
    {
        // The `requireOneSlicingArgument` argument can be used to inform the static analysis
        // that it should expect that exactly one of the defined slicing arguments is present in
        // a query. If that is not the case (i.e., if none or multiple slicing arguments are
        // present), the static analysis may throw an error.
        if (listSizeDirective is { RequireOneSlicingArgument: true })
        {
            var argumentCount = 0;

            foreach (var argumentNode in node.Arguments)
            {
                if (listSizeDirective.SlicingArguments.Contains(argumentNode.Name.Value))
                {
                    argumentCount++;
                }
            }

            if (argumentCount != 1)
            {
                // todo: lets add a validation error here and abort the cost calculation
                throw new Exception("");
            }
        }
    }
}

internal class InputCostVisitorContext
{
    public List<IInputType> Types { get; } = new();

    public List<IInputField> Fields { get; } = new();

    public List<InputObjectType> Backlog { get; } = new();

    public HashSet<IInputType> Processed { get; } = new();

    public double Cost { get; set; }

    public void Reset()
    {
        Types.Clear();
        Fields.Clear();
        Cost = 0;
    }
}

internal sealed class InputCostVisitor : SyntaxWalker<InputCostVisitorContext>
{
    public double CalculateCost(IInputField argument, ArgumentNode argumentNode, InputCostVisitorContext context)
    {
        context.Reset();

        context.Types.Push(argument.Type);
        context.Fields.Push(argument);

        Visit(argumentNode.Value, context);
        return context.Cost;
    }

    protected override ISyntaxVisitorAction Enter(
        ObjectFieldNode node,
        InputCostVisitorContext context)
    {
        if (context.Types.TryPeek(out var type) &&
            type.NamedType() is InputObjectType inputObject &&
            inputObject.Fields.TryGetField(node.Name.Value, out var field))
        {
            context.Cost += field.GetFieldWeight();
            context.Types.Push(field.Type);
            context.Fields.Push(field);
            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(
        ObjectFieldNode node,
        InputCostVisitorContext context)
    {
        context.Types.Pop();
        context.Fields.Pop();
        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        VariableNode node,
        InputCostVisitorContext context)
    {
        if (context.Types.TryPeek(out var type))
        {
            var cost = 0.0;

            var namedInputType = type.NamedType();
            if (namedInputType is InputObjectType inputObjectType)
            {
                context.Backlog.Push(inputObjectType);
                while (context.Backlog.TryPop(out var current))
                {
                    foreach (var field in current.Fields)
                    {
                        cost += field.GetFieldWeight();
                        if (field.Type.NamedType() is InputObjectType next && context.Processed.Add(next))
                        {
                            context.Backlog.Push(next);
                        }
                    }
                }
            }

            context.Cost += cost;
            return Continue;
        }

        return Skip;
    }
}
