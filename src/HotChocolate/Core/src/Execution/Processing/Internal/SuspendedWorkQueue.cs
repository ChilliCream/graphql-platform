using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class SuspendedWorkQueue
    {
        private IExecutionTask? _head;

        public bool IsEmpty { get; private set; } = true;

        public bool CopyTo(WorkQueue work, QueryPlanStateMachine stateMachine)
        {
            IExecutionTask? head = _head;
            _head = null;
            var copied = false;

            while (head is not null)
            {
                IExecutionTask current = head;
                head = head.Next;
                current.Next = null;

                if (stateMachine.IsSuspended(current))
                {
                    AppendTask(ref _head, current);
                }
                else
                {
                    work.Push(current);
                    copied = true;
                }
            }

            IsEmpty = _head is null;
            return copied;
        }

        public void Enqueue(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            AppendTask(ref _head, executionTask);
            IsEmpty = false;
        }

        private void AppendTask(ref IExecutionTask? head, IExecutionTask executionTask)
        {
            executionTask.Previous = null;
            executionTask.Next = head;
            head = executionTask;
        }

        public void Clear()
        {
            _head = null;
            IsEmpty = true;
        }
    }
}
