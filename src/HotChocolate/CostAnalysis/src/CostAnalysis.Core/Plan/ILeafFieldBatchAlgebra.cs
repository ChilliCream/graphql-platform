using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

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
