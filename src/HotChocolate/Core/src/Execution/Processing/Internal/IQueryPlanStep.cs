using System.Collections.Generic;
using System.ComponentModel;

namespace HotChocolate.Execution.Processing.Internal
{
    internal interface IQueryPlanStep
    {
        bool IsSerial { get; }

        bool IsCompleted { get; }

        IReadOnlyList<IQueryPlanStep> Steps { get; }

        IQueryPlanStep? Next { get; }


        void Initialize(IOperationContext context);

        bool IsAllowed(IExecutionTask task);
    }

    internal interface IQueryPlan
    {
        IQueryPlanStep Root { get; }

        IQueryPlanStep? GetExecutionStep(ISelection selection);

        IQueryPlanStep First { get; }
    }
}
