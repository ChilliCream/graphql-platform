using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace HotChocolate.Execution.Processing.Internal
{
    internal class QueryPlan
    {
        QueryPlanStep Root { get; }

        /// <summary>
        /// Gets the first step that needs to be executed.
        /// </summary>
        QueryPlanStep First { get; }

        // IQueryPlanStep? GetExecutionStep(ISelection selection);


    }

    internal class OperationOptimizer
    {
        public QueryPlan Optimize(IPreparedOperation operation)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class QueryPlanStep
    {
        public abstract QueryPlanStepStrategy Strategy { get; }

        public abstract IReadOnlyList<QueryPlanStep> Steps { get; }

        public QueryPlanStep? Next { get; internal set; }

        public virtual void Initialize(IOperationContext context)
        {
        }

        public virtual bool IsAllowed(IExecutionTask task) => true;
    }

    internal class ResolverQueryPlanStep : QueryPlanStep
    {
        public override QueryPlanStepStrategy Strategy { get; }

        public override IReadOnlyList<QueryPlanStep> Steps { get; }
    }

    public enum QueryPlanStepStrategy
    {
        Serial,
        Parallel
    }
}
