using System.Runtime.CompilerServices;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Compiles traversal operations into immutable plan nodes.
/// </summary>
internal sealed class PlanAlgebra :
    IAnalysisAlgebra<PlanNode>,
    IInheritedSizePlanAlgebra<PlanNode>,
    ILeafFieldBatchAlgebra<PlanNode>
{
    private readonly CostSchemaSnapshot _snapshot;
    private readonly CostAnalyses _analyses;
    private readonly CostAlgebra? _costAlgebra;
    private readonly ResponseSizeAlgebra? _responseSizeAlgebra;
    private readonly Dictionary<ConstantPlanKey, ConstantPlanNode> _constants = new(32);
    private readonly Dictionary<StaticFieldPlanKey, ConstantPlanNode> _staticFields =
        new(32, StaticFieldPlanKeyComparer.Instance);
    private readonly Dictionary<int, BatchedConstantField> _batchedFields = new(32);

    public PlanAlgebra(CostSchemaSnapshot snapshot, CostAnalyses analyses)
    {
        _snapshot = snapshot;
        _analyses = analyses;
        _costAlgebra = (analyses & CostAnalyses.Cost) != 0 ? new CostAlgebra(snapshot) : null;
        _responseSizeAlgebra = (analyses & CostAnalyses.ResponseSize) != 0
            ? new ResponseSizeAlgebra(snapshot)
            : null;
        Empty = Constant(PlanArithmetic.Empty(analyses));
    }

    public PlanNode Empty { get; }

    public PlanNode Field(in CollectedFieldGroup group, PlanNode child)
        => Field(group, inheritedSizeContext: null, child);

    public PlanNode Field(
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        PlanNode child)
    {
        if (child is ConstantPlanNode constant
            && inheritedSizeContext is not { DependsOnVariables: true }
            && group.Field is { } field)
        {
            var semanticId = GetSemanticId(group.Member);
            var key = StaticFieldPlanKey.From(field, semanticId, group.InheritedSize, constant.Value);

            if (_staticFields.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (FieldPlanNode.RequiresVariableEvaluation(
                    _snapshot,
                    group,
                    inheritedSizeContext,
                    child))
            {
                return FoldField(new FieldPlanNode(
                    _snapshot,
                    _analyses,
                    group,
                    inheritedSizeContext,
                    child));
            }

            var value = PlanArithmetic.Empty(_analyses);

            if (_costAlgebra is not null)
            {
                var cost = _costAlgebra.Field(
                    group,
                    new CostEstimate(constant.Value.FieldCost, constant.Value.TypeCost, null));
                value = new CostEstimate(cost.FieldCost, cost.TypeCost, value.MaxResponseSize);
            }

            if (_responseSizeAlgebra is not null)
            {
                value = new CostEstimate(
                    value.FieldCost,
                    value.TypeCost,
                    _responseSizeAlgebra.Field(group, constant.Value.MaxResponseSize ?? 0.0));
            }

            var result = Constant(value);
            _staticFields.Add(key, result);
            return result;
        }

        return FoldField(new FieldPlanNode(_snapshot, _analyses, group, inheritedSizeContext, child));
    }

    public PlanNode Combine(PlanNode left, PlanNode right)
        => left is ConstantPlanNode leftConstant && right is ConstantPlanNode rightConstant
            ? Constant(PlanArithmetic.Combine(leftConstant.Value, rightConstant.Value, _analyses))
            : PlanNode.Combine(left, right, _analyses);

    public PlanNode Join(PlanNode left, PlanNode right)
        => left is ConstantPlanNode leftConstant && right is ConstantPlanNode rightConstant
            ? Constant(PlanArithmetic.Join(leftConstant.Value, rightConstant.Value, _analyses))
            : PlanNode.Join(left, right, _analyses);

    public PlanNode Root(double rootTypeWeight, PlanNode selection)
        => selection is ConstantPlanNode constant
            ? Constant(PlanArithmetic.Root(rootTypeWeight, constant.Value, _analyses))
            : PlanNode.Root(rootTypeWeight, selection, _analyses);

    public void AccumulateField(
        string responseName,
        FieldNode field,
        IReadOnlyList<CollectedFieldGroupMember> members,
        SizedFieldContext? inheritedSizeContext,
        PlanNode child,
        ref bool hasValue,
        ref PlanNode value)
    {
        _batchedFields.Clear();
        var inheritedSize = InheritedListSizes.InheritedSizeFor(
            inheritedSizeContext,
            field.Name.Value);

        foreach (var member in members)
        {
            var semanticId = _snapshot.GetFieldSemanticId(member.Field);

            if (_batchedFields.TryGetValue(semanticId, out var batched))
            {
                if (batched.CanSuppressDuplicate)
                {
                    continue;
                }

                value = hasValue ? Join(value, batched.Value) : batched.Value;
                hasValue = true;
                continue;
            }

            var group = new CollectedFieldGroup(
                responseName,
                field,
                member,
                inheritedSize);
            var fieldValue = Field(group, inheritedSizeContext, child);

            if (fieldValue is ConstantPlanNode constant)
            {
                _batchedFields.Add(
                    semanticId,
                    new BatchedConstantField(
                        constant,
                        IsBitExactJoinIdempotent(constant.Value, _analyses)));
            }

            value = hasValue ? Join(value, fieldValue) : fieldValue;
            hasValue = true;
        }
    }

    private PlanNode FoldField(FieldPlanNode field)
        => field.DependsOnVariables
            ? field
            : Constant(field.Evaluate(variableValues: null));

    private ConstantPlanNode Constant(CostEstimate value)
    {
        var key = ConstantPlanKey.From(value);

        if (_constants.TryGetValue(key, out var constant))
        {
            return constant;
        }

        constant = new ConstantPlanNode(value);
        _constants.Add(key, constant);
        return constant;
    }

    private int GetSemanticId(CollectedFieldGroupMember member)
        => _snapshot.GetFieldSemanticId(member.Field);

    internal static bool IsBitExactJoinIdempotent(
        CostEstimate value,
        CostAnalyses analyses)
        => ConstantPlanKey.From(PlanArithmetic.Join(value, value, analyses))
            == ConstantPlanKey.From(value);

    private readonly record struct BatchedConstantField(
        ConstantPlanNode Value,
        bool CanSuppressDuplicate);

    private readonly record struct ConstantPlanKey(
        long FieldCost,
        long TypeCost,
        bool HasMaxResponseSize,
        long MaxResponseSize)
    {
        public static ConstantPlanKey From(CostEstimate value)
            => new(
                BitConverter.DoubleToInt64Bits(value.FieldCost),
                BitConverter.DoubleToInt64Bits(value.TypeCost),
                value.MaxResponseSize.HasValue,
                value.MaxResponseSize is { } maxResponseSize
                    ? BitConverter.DoubleToInt64Bits(maxResponseSize)
                    : 0);
    }

    private readonly record struct StaticFieldPlanKey(
        FieldNode Field,
        int SemanticId,
        bool HasInheritedSize,
        long InheritedSize,
        ConstantPlanKey Child)
    {
        public static StaticFieldPlanKey From(
            FieldNode field,
            int semanticId,
            double? inheritedSize,
            CostEstimate child)
            => new(
                field,
                semanticId,
                inheritedSize.HasValue,
                inheritedSize is { } size ? BitConverter.DoubleToInt64Bits(size) : 0,
                ConstantPlanKey.From(child));
    }

    private sealed class StaticFieldPlanKeyComparer : IEqualityComparer<StaticFieldPlanKey>
    {
        public static StaticFieldPlanKeyComparer Instance { get; } = new();

        public bool Equals(StaticFieldPlanKey x, StaticFieldPlanKey y)
            => ReferenceEquals(x.Field, y.Field)
                && x.SemanticId == y.SemanticId
                && x.HasInheritedSize == y.HasInheritedSize
                && x.InheritedSize == y.InheritedSize
                && x.Child == y.Child;

        public int GetHashCode(StaticFieldPlanKey value)
            => HashCode.Combine(
                RuntimeHelpers.GetHashCode(value.Field),
                value.SemanticId,
                value.HasInheritedSize,
                value.InheritedSize,
                value.Child);
    }
}

/// <summary>
/// Receives unresolved inherited list-size metadata while compiling a plan.
/// </summary>
internal interface IInheritedSizePlanAlgebra<TSummary>
{
    TSummary Field(
        in CollectedFieldGroup group,
        SizedFieldContext? inheritedSizeContext,
        TSummary child);
}

internal interface ILeafFieldBatchAlgebra<TSummary>
{
    void AccumulateField(
        string responseName,
        FieldNode field,
        IReadOnlyList<CollectedFieldGroupMember> members,
        SizedFieldContext? inheritedSizeContext,
        TSummary child,
        ref bool hasValue,
        ref TSummary value);
}
