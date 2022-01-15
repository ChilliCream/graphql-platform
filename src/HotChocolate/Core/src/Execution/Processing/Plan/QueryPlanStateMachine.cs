using System;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing.Tasks;

namespace HotChocolate.Execution.Processing.Plan;

internal sealed class QueryPlanStateMachine
{
    // by default we will have 8 slots to store state on.
    private State[] _stepState = { new(), new(), new(), new(), new(), new(), new(), new() };

    // the count of active query plan steps.
    private int _running;

    // the count of serial query plan steps.
    private int _serial;

    // the current operation context
    private IQueryPlanState _planState = default!;

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
    public bool IsSerial => Strategy is ExecutionStrategy.Serial;

    /// <summary>
    /// Initializes the state machine.
    /// </summary>
    /// <param name="state">
    /// The operation context.
    /// </param>
    /// <param name="plan">
    /// The query plan.
    /// </param>
    public void Initialize(IQueryPlanState state, QueryPlan plan)
    {
        _planState = state;
        _plan = plan;
        Strategy = ExecutionStrategy.Parallel;

        // we first ensure that the state machine has enough state slots for the current
        // query plan.
        if (_stepState.Length < plan.Count)
        {
            // if the query plan has more steps than we have state slots we will resize
            // the state array.
            Array.Resize(ref _stepState, plan.Count);

            // also we create new state objects for the empty slots.
            for (var i = 0; i < plan.Count; i++)
            {
                _stepState[i] ??= new();
            }
        }
    }

    public void Start()
    {
        Activate(_plan.Root);
    }

    public void TryInitializeTask(IExecutionTask task)
    {
        if (task.State is null && _plan.TryGetStep(task, out ExecutionStep? step))
        {
            task.State = step;

            if (task is ResolverTask resolverTask)
            {
                _planState.Selections.Add(resolverTask.Selection.Id);
            }
        }
    }

    public bool RegisterTask(IExecutionTask task)
    {
        if (task.State is ExecutionStep step)
        {
            State state = GetState(step);

            state.Tasks++;
            task.State = step;
            task.IsSerial = state.IsSerial;
            return state.IsActive;
        }

        return true;
    }

    public bool Complete(IExecutionTask task)
    {
        if (task.State is ExecutionStep step)
        {
            State state = GetState(step);
            step.CompleteTask(_planState, task);

            if (task.Status is not ExecutionTaskStatus.Completed)
            {
                state.Failed = true;
            }

            if (--state.Tasks == 0)
            {
                return Complete(step, !state.Failed);
            }
        }

        return false;
    }

    public bool IsSuspended(IExecutionTask task)
    {
        if (task.State is ExecutionStep step)
        {
            State state = GetState(step);
            return !state.IsActive;
        }

        return false;
    }

    public void Clear()
    {
        foreach (State? state in _stepState)
        {
            state.Clear();
        }

        _plan = default!;
        _running = default;
        _serial = default;
        _planState = default!;
    }

    private bool Activate(ExecutionStep step)
    {
        // first we try to activate the step, if that cannot be done we will mark the state
        // for this step as not active which will cause this step to be skipped.
        if (!step.TryActivate(_planState))
        {
            SetSkipped(step.Id);
            return false;
        }

        if (step is ResolverStep { Strategy: ExecutionStrategy.Serial })
        {
            _serial++;
            Strategy = ExecutionStrategy.Serial;
        }
        else if (step is SequenceStep sequence)
        {
            ExecutionStep? current = sequence.Steps[0];
            var success = false;

            while (current is not null)
            {
                if (Activate(current))
                {
                    success = true;
                    break;
                }

                current = current.Next;
            }

            if (!success)
            {
                SetSkipped(step.Id);
                return false;
            }
        }
        else if (step is ParallelStep parallel)
        {
            var allSkipped = true;

            for (var i = 0; i < parallel.Steps.Count; i++)
            {
                if (Activate(parallel.Steps[i]))
                {
                    allSkipped = false;
                }
            }

            if (allSkipped)
            {
                SetSkipped(step.Id);
                return false;
            }
        }

        SetActiveStatus(step.Id, true);
        return true;
    }

    private bool Complete(ExecutionStep step, bool success)
    {
        while (true)
        {
            SetActiveStatus(step.Id, false);

            if (step is ResolverStep { Strategy: ExecutionStrategy.Serial })
            {
                if (--_serial == 0)
                {
                    Strategy = ExecutionStrategy.Parallel;
                }
            }

            if (step.Parent is SequenceStep sequence)
            {
                if (!success && sequence.CancelOnError)
                {
                    step = sequence;
                    continue;
                }

                ExecutionStep current = step;

                while (true)
                {
                    ExecutionStep? next = current.Next;

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

            if (step.Parent is ParallelStep parallel)
            {
                success = true;

                for (var i = 0; i < parallel.Steps.Count; i++)
                {
                    if (_stepState[parallel.Steps[i].Id].IsActive)
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
        for (var i = 0; i < _plan.Count; i++)
        {
            State state = _stepState[i];

            if (state.IsActive && state.Tasks == 0)
            {
                if (_plan.TryGetStep(state.Id, out ExecutionStep? step) &&
                    step is not SequenceStep and not ParallelStep)
                {
                    if (Complete(step, !state.Failed))
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
        State state = _stepState[stepId];
        state.Id = stepId;
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
    private void SetSkipped(int stepId)
    {
        State state = _stepState[stepId];
        state.Id = stepId;
        state.IsActive = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private State GetState(ExecutionStep step)
    {
        State state = _stepState[step.Id];

        if (!state.IsInitialized)
        {
            state.Id = step.Id;
            state.IsSerial = step is ResolverStep
            { Strategy: ExecutionStrategy.Serial };
            state.IsInitialized = true;
        }

        return state;
    }

    private sealed class State
    {
        public int Id;
        public bool IsActive;
        public int Tasks;
        public bool IsSerial;
        public bool IsInitialized;
        public bool Failed = false;

        public void Clear()
        {
            Id = default;
            IsActive = default;
            IsSerial = default;
            IsInitialized = default;
            Tasks = default;
            Failed = default;
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
