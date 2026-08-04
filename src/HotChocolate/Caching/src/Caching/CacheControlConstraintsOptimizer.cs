using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Caching;

/// <summary>
/// Computes the cache control constraints for an operation during compilation.
/// </summary>
internal sealed class CacheControlConstraintsOptimizer : IOperationOptimizer
{
    public void OptimizeOperation(OperationOptimizerContext context)
    {
        if (context.Operation.Kind is not OperationType.Query
            || ContainsIntrospectionFields(context))
        {
            return;
        }

        var constraints = CacheControlConstraintsComputer.Compute(context.Operation);

        if (constraints is not null)
        {
            var headerValue = CacheControlConstraintsComputer.CreateHeaderValue(constraints);
            context.Operation.Features.SetSafe(constraints);
            context.Operation.Features.SetSafe(headerValue);
        }
    }

    private static bool ContainsIntrospectionFields(OperationOptimizerContext context)
    {
        var selections = context.Operation.RootSelectionSet.Selections;

        foreach (var selection in selections)
        {
            var field = selection.Field;
            if (field.IsIntrospectionField
                && !field.Name.EqualsOrdinal(IntrospectionFieldNames.TypeName))
            {
                return true;
            }
        }

        return false;
    }
}
