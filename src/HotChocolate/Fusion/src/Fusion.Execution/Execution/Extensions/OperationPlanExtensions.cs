using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;

namespace HotChocolate.Fusion.Execution;

internal static class OperationPlanExtensions
{
    extension(OperationPlan plan)
    {
        /// <summary>
        /// Gets the reuse candidate map of the plan, or <c>null</c> if no lookup of the plan
        /// has reuse candidates.
        /// </summary>
        public EntityReuseCandidateMap? EntityReuseCandidateMap
            => plan.Operation.Features.Get<EntityReuseCandidateMap>();
    }
}
