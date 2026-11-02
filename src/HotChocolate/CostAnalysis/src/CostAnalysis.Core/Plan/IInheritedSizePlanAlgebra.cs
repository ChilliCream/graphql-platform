namespace HotChocolate.CostAnalysis;

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
