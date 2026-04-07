using HotChocolate.Caching;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion.Caching;

/// <summary>
/// An <see cref="IOperationPlannerInterceptor"/> that computes cache control constraints
/// from <c>@cacheControl</c> directives on the composite schema after the operation plan
/// is completed, and stores the computed constraints and HTTP header value on the
/// operation's features for the <see cref="QueryCacheMiddleware"/> to consume.
/// </summary>
internal sealed class CacheControlPlannerInterceptor : IOperationPlannerInterceptor
{
    public void OnAfterPlanCompleted(
        OperationDocumentInfo operationDocumentInfo,
        OperationPlan operationPlan)
    {
        var constraints = CacheControlConstraintsComputer.Compute(operationPlan.Operation);

        if (constraints is not null)
        {
            var headerValue = CacheControlConstraintsComputer.CreateHeaderValue(constraints);
            operationPlan.Operation.Features.Set(constraints);
            operationPlan.Operation.Features.Set(headerValue);
        }
    }
}
