using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;

namespace HotChocolate.Fusion.Execution;

internal static class OperationPlanExtensions
{
    extension(OperationPlan plan)
    {
        /// <summary>
        /// Gets the entity reuse index of the plan, or <c>null</c> if no lookup of the plan
        /// can reuse entities.
        /// </summary>
        public EntityReuseIndex? EntityReuseIndex
            => plan.Operation.Features.Get<EntityReuseIndex>();
    }
}
