using System;
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

        // the count of serial query plan steps.
        private int _serial;

        // the current operation context
        private IOperationContext _context = default!;

        // the current query plan
        private QueryPlan _plan = default!;

        /// <summary>
        /// Defines if processing the query plan is completed.
        /// </summary>
        public bool IsCompleted => _running == 0;

        /// <summary>
        /// The current execution strategy for the main task processor.
        /// </summary>
        public ExecutionStrategy Strategy { get; private set; }

        /// <summary>
        /// Defines if the main processor execution strategy is serial.
        /// </summary>
        public bool IsSerial => Strategy == ExecutionStrategy.Serial;

        /// <summary>
        /// Initializes the state machine.
        /// </summary>
        /// <param name="context">
        /// The operation context.
        /// </param>
        /// <param name="plan">
        /// The query plan.
        /// </param>
        public void Initialize(IOperationContext context, QueryPlan plan)
        {
            _context = context;
            _plan = plan;
            Strategy = ExecutionStrategy.Parallel;

            // we first ensure that the state machine has enough state slots for the current
            // query plan.
            if (_state.Length < plan.Count)
            {
                // if the query plan has more steps than we have state slots we will resize
                // the state array.
                Array.Resize(ref _state, plan.Count);

                // also we create new state objects for the empty slots.
                for (var i = 0; i < _state.Length; i++)
                {
                    _state[i] ??= new();
                }
            }

            // last we activate the query plan by activating the first steps.
            Activate(plan.Root);
        }

        public bool Register(IExecutionTask task)
        {
            if (_plan.TryGetStep(task, out var step))
            {
                InitializeState(step, out State state);

                state.Tasks++;
                task.State = step;
                task.IsSerial = state.IsSerial;
                return state.IsActive;
            }

            return true;
        }

        public bool Complete(IExecutionTask task)
        {
            if (task.State is QueryPlanStep step)
            {
                InitializeState(step, out State state);

                if (--state.Tasks == 0)
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
                InitializeState(step, out State state);
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
            _serial = default;
        }

        private bool Activate(QueryPlanStep step)
        {
            while (true)
            {
                if (!step.Initialize(_context))
                {
                    SetActiveStatus(step.Id, true);
                    SetActiveStatus(step.Id, false);
                    return false;
                }

                if (step is ResolverQueryPlanStep { Strategy: ExecutionStrategy.Serial })
                {
                    _serial++;
                    Strategy = ExecutionStrategy.Serial;
                }

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

            return true;
        }

        private bool Complete(QueryPlanStep step)
        {
            while (true)
            {
                SetActiveStatus(step.Id, false);

                if (step is ResolverQueryPlanStep { Strategy: ExecutionStrategy.Serial })
                {
                    if (--_serial == 0)
                    {
                        Strategy = ExecutionStrategy.Parallel;
                    }
                }

                if (step.Parent is SequenceQueryPlanStep sequence)
                {
                    QueryPlanStep current = step;

                    while (true)
                    {
                        QueryPlanStep? next = sequence.GetNextStep(current);

                        if (next is null)
                        {
                            break;
                        }

                        if (Activate(next))
                        {
                            return true;
                        }

                        current = next;
                    }

                    step = sequence;
                    continue;
                }

                if (step.Parent is ParallelQueryPlanStep parallel)
                {
                    for (var i = 0; i < parallel.Steps.Count; i++)
                    {
                        if (_state[parallel.Steps[i].Id].IsActive)
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

        public bool CompleteNext()
        {
TryAgain:
            for (var i = 0; i < _state.Length; i++)
            {
                var state = _state[i];

                if (state.IsActive && state.Tasks == 0)
                {
                    if (_plan.TryGetStep(state.Id, out QueryPlanStep? step) &&
                        step is not SequenceQueryPlanStep and not ParallelQueryPlanStep)
                    {
                        if (Complete(step))
                        {
                            return true;
                        }

                        goto TryAgain;
                    }
                }
            }

            return false;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeState(QueryPlanStep step, out State state)
        {
            state = _current!;

            if (state is null! || state.Id != step.Id || !state.IsInitialized)
            {
                _current = state = _state[step.Id];
                if (!state.IsInitialized)
                {
                    state.Id = step.Id;
                    state.IsSerial = step is ResolverQueryPlanStep
                        { Strategy: ExecutionStrategy.Serial };
                    state.IsInitialized = true;
                }
            }
        }

        private sealed class State
        {
            public int Id;
            public bool IsActive;
            public int Tasks;
            public bool IsSerial;
            public bool IsInitialized;

            public void Clear()
            {
                Id = default;
                IsActive = default;
                IsSerial = default;
                IsInitialized = default;
                Tasks = default;
            }

            /// <summary>
            /// Debug visualization.
            /// </summary>
            public override string ToString()
            {
                string active = IsActive ? " active" : "";
                string tasks = Tasks > 0 ? $" tasks: {Tasks}" : "";
                return $"{Id}{active}{tasks}";
            }
        }
    }
}
