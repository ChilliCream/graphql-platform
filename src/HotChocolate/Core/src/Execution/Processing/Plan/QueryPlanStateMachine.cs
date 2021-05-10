using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Execution.Processing.Plan
{
    internal class QueryPlanStateMachine
    {
        private readonly HashSet<QueryPlanStep> _active = new();
        private int[] _tasks = Array.Empty<int>();
        private QueryPlan _plan = default!;

        public bool IsCompleted => _active.Count == 0;

        public void Initialize(QueryPlan plan)
        {
            _plan = plan;

            if (_tasks.Length < plan.Count)
            {
                _tasks = new int[plan.Count];
            }

            Activate(plan.Root);
        }

        public bool Register(IExecutionTask task)
        {
            if (_plan.TryGetStep(task, out var step))
            {
                task.State = step;
                _tasks[step.Id]++;
                return _active.Contains(step);
            }

            return true;
        }

        public bool Complete(IExecutionTask task)
        {
            if (task.State is QueryPlanStep step)
            {
                if (--_tasks[step.Id] == 0)
                {
                    return Complete(step);
                }
            }

            return false;
        }

        public bool IsSuspended(IExecutionTask task) =>
            task.State is QueryPlanStep step &&
            _active.Contains(step);

        public void Clear()
        {
            _active.Clear();
            _tasks.AsSpan().Clear();
        }

        private void Activate(QueryPlanStep step)
        {
            if (step is SequenceQueryPlanStep sequence)
            {
                _active.Add(sequence);
                Activate(sequence.Steps[0]);
            }
            else if (step is ParallelQueryPlanStep parallel)
            {
                _active.Add(parallel);

                for (var i = 0; i < parallel.Steps.Count; i++)
                {
                    Activate(parallel.Steps[i]);
                }
            }
            else
            {
                _active.Add(step);
            }
        }

        private bool Complete(QueryPlanStep step)
        {
            _active.Remove(step);

            if (step.Parent is SequenceQueryPlanStep sequence)
            {
                if (sequence.GetNextStep(sequence) is { } next)
                {
                    Activate(next);
                    return true;
                }

                return Complete(sequence);
            }

            if (step.Parent is ParallelQueryPlanStep parallel)
            {
                if (parallel.Steps.Intersect(_active).Any())
                {
                    return false;
                }

                return Complete(parallel);
            }

            return false;
        }
    }
}
