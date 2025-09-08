using System.Runtime.CompilerServices;
using HotChocolate.CostAnalysis.Types;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.CostAnalysis;

internal sealed class CostAnalyzer(RequestCostOptions options) : TypeDocumentValidatorVisitor
{
    public CostMetrics Analyze(OperationDefinitionNode operation, DocumentValidatorContext context)
    {
        var feature = context.Features.GetOrSet<CostContext>();

        Visit(operation, context);

        var summary = feature.SelectionSetCost[operation.SelectionSet];

        return new CostMetrics { TypeCost = summary.TypeCost, FieldCost = summary.FieldCost };
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        context.GetFieldSets().Clear();
        context.SelectionSets.Clear();

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        // add operation cost
        var costContext = context.GetCostContext();
        var type = context.Types.Peek();
        var cost = type.GetTypeWeight();
        var summary = costContext.SelectionSetCost[node.SelectionSet];
        summary.TypeCost += cost;

        return base.Leave(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        var selectionSet = context.SelectionSets.Peek();

        if (!context.GetFieldSets().TryGetValue(selectionSet, out var fields))
        {
            fields = context.RentFieldInfoList();
            context.GetFieldSets().Add(selectionSet, fields);
        }

        if (IntrospectionFieldNames.TypeName.EqualsOrdinal(node.Name.Value))
        {
            fields.Add(new FieldInfo(context.Types.Peek(), context.NullStringType(), node));
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
        DocumentValidatorContext context)
    {
        if (!context.Path.TryPeek(out var parent))
        {
            return Continue;
        }

        if (parent.Kind is SyntaxKind.OperationDefinition or SyntaxKind.Field)
        {
            context.SelectionSets.Pop();

            if (!context.GetFieldSets().TryGetValue(node, out var fields))
            {
                return Continue;
            }

            var costContext = context.GetCostContext();
            if (!costContext.SelectionSetCost.TryGetValue(node, out var costSummary))
            {
                costSummary = new CostSummary();
                costContext.SelectionSetCost.Add(node, costSummary);
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
                    var result = CalculateSelectionSetCost(possibleType, fields, context);

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
        DocumentValidatorContext context)
    {
        if (context.Fragments.TryEnter(node, out var fragment))
        {
            var result = Visit(fragment, node, context);
            context.Fragments.Leave(fragment);

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

    private (double TypeCost, double FieldCost) CalculateSelectionSetCost(
        IObjectTypeDefinition possibleType,
        List<FieldInfo> fields,
        DocumentValidatorContext context)
    {
        var costContext = context.GetCostContext();
        var processed = costContext.Processed;
        var inputCostVisitor = costContext.InputCostVisitor;

        var typeCostSum = 0.0;
        var fieldCostSum = 0.0;

        for (var i = 0; i < fields.Count; i++)
        {
            var fieldInfo = fields[i];
            var fieldNode = fieldInfo.SyntaxNode;

            if (processed.Add(fieldInfo.ResponseName)
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
                var listSizeDirective = field.Directives.FirstOrDefaultValue<ListSizeDirective>();

                listSizeDirective.ValidateRequireOneSlicingArgument(fieldNode, context.Path);

                if (fieldNode.Arguments.Count > 0)
                {
                    foreach (var argumentNode in fieldNode.Arguments)
                    {
                        if (field.Arguments.TryGetField(argumentNode.Name.Value, out var argument))
                        {
                            var argumentCost = argument.GetFieldWeight();
                            inputCostVisitor.CalculateCost(argument, argumentNode, costContext.InputCostVisitorContext);
                            argumentCost += costContext.InputCostVisitorContext.Cost;

                            if ((argument.Flags & FieldFlags.FilterArgument) == FieldFlags.FilterArgument
                                && argumentNode.Value.Kind == SyntaxKind.Variable
                                && options.FilterVariableMultiplier.HasValue)
                            {
                                argumentCost *= options.FilterVariableMultiplier.Value;
                            }

                            fieldCost += argumentCost;
                        }
                    }
                }

                if (field.Type.NamedType().IsCompositeType() && fieldNode.SelectionSet is not null)
                {
                    var selectionSetCostSummary = context.GetSelectionSetCost(fieldNode.SelectionSet);
                    selectionSetCost += selectionSetCostSummary.FieldCost;
                    typeCost += selectionSetCostSummary.TypeCost;
                }

                if (field.Type.IsListType())
                {
                    var arguments = fieldNode.Arguments;
                    var parentField = context.OutputFields.PeekOrDefault();

                    if (listSizeDirective is null && parentField is not null)
                    {
                        var parentListSizeDirective = parentField.Directives.FirstOrDefaultValue<ListSizeDirective>();

                        if (parentListSizeDirective?.SizedFields.Contains(field.Name) ?? false)
                        {
                            listSizeDirective = parentListSizeDirective;
                            arguments = context.Path.PeekOrDefault<ISyntaxNode, FieldNode>()!.Arguments;
                        }
                    }

                    // if the field is a list type, we are multiplying the cost
                    // by the estimated list size.
                    var listSize = field.GetListSize(arguments, listSizeDirective);
                    typeCost *= listSize;
                    selectionSetCost *= listSize;
                }

                fieldCost += selectionSetCost;

                // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost
                // https://ibm.github.io/graphql-specs/cost-spec.html#sel-IALJJHPDABAB4Biqc
                // Second, if this sum is negative then round it up to zero
                if (fieldCost < 0)
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
        => fieldName.EqualsOrdinal(IntrospectionFieldNames.TypeName);
}

file sealed class CostContext : ValidatorFeature
{
    private static readonly FieldInfoListBufferPool s_fieldInfoPool = new();
    private readonly List<FieldInfoListBuffer> _buffers = [new()];

    public IType NonNullString { get; private set; } = null!;

    public Dictionary<SelectionSetNode, List<FieldInfo>> FieldSets { get; } = [];

    public readonly Dictionary<SelectionSetNode, CostSummary> SelectionSetCost = [];

    public readonly HashSet<string> Processed = [];

    public readonly InputCostVisitor InputCostVisitor = new();

    public InputCostVisitorContext InputCostVisitorContext { get; } = new();

    public List<FieldInfo> RentFieldInfoList()
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
        FieldSets.Clear();

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

        InputCostVisitorContext.Clear();
    }
}

file static class DocumentValidatorContextExtensions
{
    public static IType NullStringType(this DocumentValidatorContext context)
        => context.Features.GetRequired<CostContext>().NonNullString;

    public static Dictionary<SelectionSetNode, List<FieldInfo>> GetFieldSets(this DocumentValidatorContext context)
        => context.Features.GetRequired<CostContext>().FieldSets;

    public static List<FieldInfo> RentFieldInfoList(this DocumentValidatorContext context)
        => context.Features.GetRequired<CostContext>().RentFieldInfoList();

    public static CostContext GetCostContext(this DocumentValidatorContext context)
        => context.Features.GetRequired<CostContext>();

    public static CostSummary GetSelectionSetCost(
        this DocumentValidatorContext context,
        SelectionSetNode selectionSetNode)
        => context.GetCostContext().SelectionSetCost[selectionSetNode];
}

file sealed class CostSummary
{
    public double TypeCost { get; set; }
    public double FieldCost { get; set; }
}
