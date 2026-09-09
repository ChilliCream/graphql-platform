using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One immutable node in a compiled cost-plan expression.
/// </summary>
internal abstract class PlanNode
{
    public abstract bool DependsOnVariables { get; }

    public abstract CostEstimate Evaluate(ICostVariableValues? variableValues);

    public static PlanNode Constant(CostEstimate value) => new ConstantPlanNode(value);

    public static PlanNode Combine(PlanNode left, PlanNode right, CostAnalyses analyses)
        => FoldBinary(left, right, analyses, isJoin: false);

    public static PlanNode Join(PlanNode left, PlanNode right, CostAnalyses analyses)
        => FoldBinary(left, right, analyses, isJoin: true);

    public static PlanNode Root(double rootTypeWeight, PlanNode selection, CostAnalyses analyses)
    {
        if (!selection.DependsOnVariables)
        {
            return Constant(PlanArithmetic.Root(rootTypeWeight, selection.Evaluate(null), analyses));
        }

        return new RootPlanNode(rootTypeWeight, selection, analyses);
    }

    public static PlanNode Condition(
        string variableName,
        PlanNode whenFalse,
        PlanNode whenTrue,
        CostAnalyses analyses)
    {
        if (whenFalse is ConstantPlanNode falseConstant
            && whenTrue is ConstantPlanNode trueConstant
            && falseConstant.Value == trueConstant.Value)
        {
            return falseConstant;
        }

        return new ConditionPlanNode(variableName, whenFalse, whenTrue, analyses);
    }

    private static PlanNode FoldBinary(
        PlanNode left,
        PlanNode right,
        CostAnalyses analyses,
        bool isJoin)
    {
        if (!left.DependsOnVariables && !right.DependsOnVariables)
        {
            var leftValue = left.Evaluate(null);
            var rightValue = right.Evaluate(null);
            return Constant(isJoin
                ? PlanArithmetic.Join(leftValue, rightValue, analyses)
                : PlanArithmetic.Combine(leftValue, rightValue, analyses));
        }

        return new BinaryPlanNode(left, right, analyses, isJoin);
    }
}

internal sealed class ConstantPlanNode(CostEstimate value) : PlanNode
{
    public CostEstimate Value { get; } = value;

    public override bool DependsOnVariables => false;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues) => Value;
}

internal sealed class BinaryPlanNode(
    PlanNode left,
    PlanNode right,
    CostAnalyses analyses,
    bool isJoin) : PlanNode
{
    public override bool DependsOnVariables => left.DependsOnVariables || right.DependsOnVariables;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
    {
        var leftValue = left.Evaluate(variableValues);
        var rightValue = right.Evaluate(variableValues);
        return isJoin
            ? PlanArithmetic.Join(leftValue, rightValue, analyses)
            : PlanArithmetic.Combine(leftValue, rightValue, analyses);
    }
}

internal sealed class RootPlanNode(
    double rootTypeWeight,
    PlanNode selection,
    CostAnalyses analyses) : PlanNode
{
    public override bool DependsOnVariables => selection.DependsOnVariables;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
        => PlanArithmetic.Root(rootTypeWeight, selection.Evaluate(variableValues), analyses);
}

internal sealed class ConditionPlanNode(
    string variableName,
    PlanNode whenFalse,
    PlanNode whenTrue,
    CostAnalyses analyses) : PlanNode
{
    public override bool DependsOnVariables => true;

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
    {
        if (variableValues is null)
        {
            return PlanArithmetic.Join(
                whenFalse.Evaluate(null),
                whenTrue.Evaluate(null),
                analyses);
        }

        var value = variableValues.TryGetValue(variableName, out var node)
            && node is BooleanValueNode boolean
            && boolean.Value;
        return (value ? whenTrue : whenFalse).Evaluate(variableValues);
    }
}

internal sealed class FieldPlanNode : PlanNode
{
    private readonly CostSchemaSnapshot _snapshot;
    private readonly CostAnalyses _analyses;
    private readonly PlanNode _child;
    private readonly IType _outputType;
    private readonly ListSizeMetadata? _listSizeMetadata;
    private readonly IReadOnlyDictionary<string, SlicingArgumentValue> _slicingArguments;
    private readonly SizedFieldContext? _inheritedSizeContext;
    private readonly double? _fixedInheritedSize;
    private readonly string _fieldName;
    private readonly double _fieldWeight;
    private readonly double _returnTypeWeight;
    private readonly InputValueOperand[] _arguments;
    private readonly InputValueOperand[] _directiveArguments;

    public FieldPlanNode(
        CostSchemaSnapshot snapshot,
        CostAnalyses analyses,
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        PlanNode child)
    {
        _snapshot = snapshot;
        _analyses = analyses;
        _child = child;
        _outputType = group.Member.Field.Type;
        _fieldName = group.Member.Field.Name;
        _listSizeMetadata = snapshot.GetListSizeMetadata(group.Member.ParentType.Name, _fieldName);
        _slicingArguments = SlicingArgumentValues.Build(
            _listSizeMetadata,
            group.Member.Field,
            group.Field!.Arguments);
        _inheritedSizeContext = inheritedSizeContext;
        _fixedInheritedSize = group.InheritedSize;
        _fieldWeight = snapshot.GetFieldWeight(group.Member.ParentType.Name, _fieldName);
        _returnTypeWeight = snapshot.GetTypeWeight(group.Member.Field.Type.NamedType().Name);
        _arguments = BuildOperands(
            snapshot.GetFieldArguments(group.Member.ParentType.Name, _fieldName),
            group.Field.Arguments);
        _directiveArguments = BuildDirectiveOperands(snapshot, group.Field.Directives);
        DependsOnVariables = child.DependsOnVariables
            || ContainsVariable(_slicingArguments)
            || ContainsVariable(_arguments)
            || ContainsVariable(_directiveArguments)
            || inheritedSizeContext is { DependsOnVariables: true };
    }

    public override bool DependsOnVariables { get; }

    public override CostEstimate Evaluate(ICostVariableValues? variableValues)
    {
        var child = _child.Evaluate(variableValues);
        var inheritedSize = _fixedInheritedSize;

        if (_inheritedSizeContext is { } inherited
            && inherited.TryResolve(_fieldName, variableValues, out var resolvedSize))
        {
            inheritedSize = resolvedSize;
        }

        ReadOnlySpan<double> inheritedSizes = inheritedSize is { } size ? [size] : [];
        var multiplier = ListSizeResolver.Resolve(
            _outputType.IsListType(),
            _listSizeMetadata,
            inheritedSizes,
            _slicingArguments,
            variableValues,
            _snapshot.Options.DefaultListSize);

        var cost = PlanArithmetic.Empty(_analyses);

        if ((_analyses & CostAnalyses.Cost) != 0)
        {
            var estimate = CostFieldRule.Field(
                multiplier,
                _fieldWeight,
                ComputeInputCost(_arguments, variableValues),
                ComputeInputCost(_directiveArguments, variableValues),
                _returnTypeWeight,
                new CostEstimate(child.FieldCost, child.TypeCost, null));
            estimate = CostFieldRule.Join(CostFieldRule.Empty, estimate);
            cost = new CostEstimate(estimate.FieldCost, estimate.TypeCost, cost.MaxResponseSize);
        }

        if ((_analyses & CostAnalyses.ResponseSize) != 0)
        {
            cost = new CostEstimate(
                cost.FieldCost,
                cost.TypeCost,
                ResponseSizeFieldRule.Field(
                    _outputType,
                    multiplier,
                    child.MaxResponseSize ?? ResponseSizeFieldRule.Empty));
        }

        return cost;
    }

    private double ComputeInputCost(InputValueOperand[] operands, ICostVariableValues? variableValues)
    {
        var cost = 0.0;

        foreach (var operand in operands)
        {
            cost += InputCost.Compute(_snapshot, operand.Definition, operand.Value, variableValues);
        }

        return cost;
    }

    private static InputValueOperand[] BuildOperands(
        IReadOnlyList<InputValueMetadata> definitions,
        IReadOnlyList<ArgumentNode> arguments)
    {
        if (definitions.Count == 0)
        {
            return [];
        }

        var result = new InputValueOperand[definitions.Count];

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            result[i] = new InputValueOperand(
                definition,
                SlicingArgumentValues.FindArgumentValue(arguments, definition.Name));
        }

        return result;
    }

    private static InputValueOperand[] BuildDirectiveOperands(
        CostSchemaSnapshot snapshot,
        IReadOnlyList<DirectiveNode> directives)
    {
        var count = 0;

        foreach (var directive in directives)
        {
            if (snapshot.TryGetDirectiveArgumentMetadata(directive.Name.Value, out var definitions))
            {
                count += definitions.Length;
            }
        }

        if (count == 0)
        {
            return [];
        }

        var result = new InputValueOperand[count];
        var index = 0;

        foreach (var directive in directives)
        {
            if (!snapshot.TryGetDirectiveArgumentMetadata(directive.Name.Value, out var definitions))
            {
                continue;
            }

            foreach (var definition in definitions)
            {
                result[index++] = new InputValueOperand(
                    definition,
                    SlicingArgumentValues.FindArgumentValue(directive.Arguments, definition.Name));
            }
        }

        return result;
    }

    private static bool ContainsVariable(
        IReadOnlyDictionary<string, SlicingArgumentValue> arguments)
    {
        foreach (var argument in arguments.Values)
        {
            if (ContainsVariable(argument.SuppliedValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsVariable(InputValueOperand[] operands)
    {
        foreach (var operand in operands)
        {
            if (ContainsVariable(operand.Value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsVariable(IValueNode? value)
        => value switch
        {
            VariableNode => true,
            ListValueNode list => list.Items.Any(ContainsVariable),
            ObjectValueNode inputObject => inputObject.Fields.Any(field => ContainsVariable(field.Value)),
            _ => false
        };

    private readonly record struct InputValueOperand(InputValueMetadata Definition, IValueNode? Value);
}

internal static class PlanArithmetic
{
    public static CostEstimate Empty(CostAnalyses analyses)
        => new(0.0, 0.0, (analyses & CostAnalyses.ResponseSize) != 0 ? 0.0 : null);

    public static CostEstimate Combine(CostEstimate left, CostEstimate right, CostAnalyses analyses)
        => new(
            (analyses & CostAnalyses.Cost) != 0 ? left.FieldCost + right.FieldCost : 0.0,
            (analyses & CostAnalyses.Cost) != 0 ? left.TypeCost + right.TypeCost : 0.0,
            (analyses & CostAnalyses.ResponseSize) != 0
                ? ResponseSizeFieldRule.Combine(left.MaxResponseSize ?? 0.0, right.MaxResponseSize ?? 0.0)
                : null);

    public static CostEstimate Join(CostEstimate left, CostEstimate right, CostAnalyses analyses)
        => new(
            (analyses & CostAnalyses.Cost) != 0 ? double.MaxNumber(left.FieldCost, right.FieldCost) : 0.0,
            (analyses & CostAnalyses.Cost) != 0 ? double.MaxNumber(left.TypeCost, right.TypeCost) : 0.0,
            (analyses & CostAnalyses.ResponseSize) != 0
                ? ResponseSizeFieldRule.Join(left.MaxResponseSize ?? 0.0, right.MaxResponseSize ?? 0.0)
                : null);

    public static CostEstimate Root(double rootTypeWeight, CostEstimate selection, CostAnalyses analyses)
        => new(
            selection.FieldCost,
            (analyses & CostAnalyses.Cost) != 0
                ? CostFieldRule.Clamp0(rootTypeWeight + selection.TypeCost)
                : 0.0,
            selection.MaxResponseSize);
}
