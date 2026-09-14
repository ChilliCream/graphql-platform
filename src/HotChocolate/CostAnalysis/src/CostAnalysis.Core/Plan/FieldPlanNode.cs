using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

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

    internal static bool RequiresVariableEvaluation(
        CostSchemaSnapshot snapshot,
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        PlanNode child)
    {
        if (child.DependsOnVariables
            || inheritedSizeContext is { DependsOnVariables: true }
            || group.Field is not { } field)
        {
            return true;
        }

        var parentTypeName = group.Member.ParentType.Name;
        var fieldName = group.Member.Field.Name;
        var listSizeMetadata = snapshot.GetListSizeMetadata(parentTypeName, fieldName);

        if (listSizeMetadata is { SlicingArguments.Length: > 0 })
        {
            foreach (var name in listSizeMetadata.SlicingArguments)
            {
                if (ContainsVariable(SlicingArgumentValues.FindArgumentValue(field.Arguments, name)))
                {
                    return true;
                }
            }
        }

        foreach (var definition in snapshot.GetFieldArguments(parentTypeName, fieldName))
        {
            if (ContainsVariable(SlicingArgumentValues.FindArgumentValue(field.Arguments, definition.Name)))
            {
                return true;
            }
        }

        foreach (var directive in field.Directives)
        {
            if (!snapshot.TryGetDirectiveArgumentMetadata(directive.Name.Value, out var definitions))
            {
                continue;
            }

            foreach (var definition in definitions)
            {
                if (ContainsVariable(
                    SlicingArgumentValues.FindArgumentValue(directive.Arguments, definition.Name)))
                {
                    return true;
                }
            }
        }

        return false;
    }

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
            _snapshot.DefaultListSize);

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
    {
        switch (value)
        {
            case VariableNode:
                return true;

            case ListValueNode list:
                foreach (var item in list.Items)
                {
                    if (ContainsVariable(item))
                    {
                        return true;
                    }
                }

                break;

            case ObjectValueNode inputObject:
                foreach (var field in inputObject.Fields)
                {
                    if (ContainsVariable(field.Value))
                    {
                        return true;
                    }
                }

                break;
        }

        return false;
    }

    private readonly record struct InputValueOperand(InputValueMetadata Definition, IValueNode? Value);
}
