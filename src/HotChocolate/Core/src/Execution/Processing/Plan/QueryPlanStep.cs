using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Execution.Processing.Plan
{
    internal abstract class QueryPlanStep
    {
        private static readonly QueryPlanStep[] _empty = Array.Empty<QueryPlanStep>();

        internal int Id { get; set; }

        protected internal virtual string Name => GetType().Name;

        public QueryPlanStep? Parent { get; internal set; }

        internal virtual IReadOnlyList<QueryPlanStep> Steps => _empty;

        public virtual bool Initialize(IOperationContext context) => true;

        internal bool IsPartOf(QueryPlanStep step)
        {
            if (Steps.Count == 0)
            {
                return false;
            }

            for(var i = 0; i < Steps.Count; i++)
            {
                if (ReferenceEquals(Steps[i], step))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool IsPartOf(IExecutionTask task)
        {
            if (Steps.Count == 0)
            {
                return false;
            }

            for(var i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].IsPartOf(task))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryGetStep(
            IExecutionTask executionTask,
            [MaybeNullWhen(false)] out QueryPlanStep step)
        {
            if (Steps.Count == 0 && IsPartOf(executionTask))
            {
                step = this;
                return true;
            }

            for(var i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].TryGetStep(executionTask, out step))
                {
                    return true;
                }
            }

            step = null;
            return false;
        }

        internal bool TryGetStep(
            int stepId,
            [MaybeNullWhen(false)] out QueryPlanStep step)
        {
            if (Id == stepId)
            {
                step = this;
                return true;
            }

            for(var i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].TryGetStep(stepId, out step))
                {
                    return true;
                }
            }

            step = null;
            return false;
        }
    }
}
