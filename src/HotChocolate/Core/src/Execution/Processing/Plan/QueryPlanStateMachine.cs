using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing.Plan
{
    internal sealed class QueryPlanStateMachine
    {
        // by default we will have 8 slots to store state on.
        private State[] _state = { new(), new(), new(), new(), new(), new(), new(), new() };

        // since it is likely that we hit one specific state multiple time we keep the last touched
        // cached for faster access.
        private State? _current;

        // the count of active query plan steps.
        private int _running;

        // the current operation context
        private IOperationContext _context = default!;

        // the current query plan
        private QueryPlan _plan = default!;

        public bool IsCompleted => _running == 0;

        public void Initialize(IOperationContext context, QueryPlan plan)
        {
            _context = context;
            _plan = plan;

            if (_state.Length < plan.Count)
            {
                Array.Resize(ref _state, plan.Count);
            }

            Activate(plan.Root);
        }

        public bool Register(IExecutionTask task)
        {
            if (_plan.TryGetStep(task, out var step))
            {
                State? state = _current;

                if (state is null || state.Id != step.Id)
                {
                    _current = state = _state[step.Id];
                    state.Id = step.Id;
                }

                state.Tasks++;
                task.State = step;
                return state.IsActive;
            }

            return true;
        }

        public bool Complete(IExecutionTask task)
        {
            if (task.State is QueryPlanStep step)
            {
                State? state = _current;

                if (state is null || state.Id != step.Id)
                {
                    _current = state = _state[step.Id];
                    state.Id = step.Id;
                }

                if (--state.Tasks == 0)
                {
                    return Complete(step);
                }
            }

            return false;
        }

        public bool CompleteNext()
        {
            for (var i = 0; i < _state.Length; i++)
            {
                State state = _state[i];

                if (_state[i].IsActive &&
                    _plan.TryGetStep(state.Id, out QueryPlanStep? step) &&
                    step is not SequenceQueryPlanStep and not ParallelQueryPlanStep)
                {
                    return Complete(step);
                }
            }

            return false;
        }

        public bool IsSuspended(IExecutionTask task)
        {
            if (task.State is QueryPlanStep step)
            {
                State? state = _current;

                if (state is null || state.Id != step.Id)
                {
                    _current = state = _state[step.Id];
                    state.Id = step.Id;
                }

                return !state.IsActive;
            }

            return false;
        }

        public void Clear()
        {
            for (var i = 0; i < _state.Length; i++)
            {
                _state[i].Clear();
            }

            _current = _state[0];
            _plan = default!;
            _running = default;
        }

        private void Activate(QueryPlanStep step)
        {
            while (true)
            {
                step.Initialize(_context);

                if (step is SequenceQueryPlanStep sequence)
                {
                    SetActiveStatus(sequence.Id, true);
                    step = sequence.Steps[0];
                    continue;
                }

                if (step is ParallelQueryPlanStep parallel)
                {
                    SetActiveStatus(parallel.Id, true);

                    for (var i = 0; i < parallel.Steps.Count; i++)
                    {
                        Activate(parallel.Steps[i]);
                    }

                    break;
                }

                SetActiveStatus(step.Id, true);
                break;
            }
        }

        private bool Complete(QueryPlanStep step)
        {
            while (true)
            {
                SetActiveStatus(step.Id, false);

                if (step.Parent is SequenceQueryPlanStep sequence)
                {
                    if (sequence.GetNextStep(step) is { } next)
                    {
                        Activate(next);
                        return true;
                    }

                    step = sequence;
                    continue;
                }

                if (step.Parent is ParallelQueryPlanStep parallel)
                {
                    for(var i = 0; i < parallel.Steps.Count; i++)
                    {
                        if (_state[i].IsActive)
                        {
                            return false;
                        }
                    }

                    step = parallel;
                    continue;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetActiveStatus(int stepId, bool active)
        {
            State? state = _current;

            if (state is null || state.Id != stepId)
            {
                _current = state = _state[stepId];
                state.Id = stepId;
            }

            state.IsActive = active;

            if (active)
            {
                _running++;
            }
            else
            {
                _running--;
            }
        }

        private sealed class State
        {
            public int Id;
            public bool IsActive;
            public int Tasks;

            public void Clear()
            {
                Id = default;
                IsActive = default;
                Tasks = default;
            }
        }
    }
}
