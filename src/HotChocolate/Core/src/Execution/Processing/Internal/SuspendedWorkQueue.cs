using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class SuspendedWorkQueue
    {
        private IExecutionTask? _head;

        public bool IsEmpty { get; private set; } = true;

        public void CopyTo(WorkQueue work, WorkQueue serial, QueryPlanStateMachine stateMachine)
        {
            IExecutionTask? head = _head;
            _head = null;

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
                    (current.IsSerial ? serial : work).Push(current);
                }
            }

            IsEmpty = _head is null;
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
