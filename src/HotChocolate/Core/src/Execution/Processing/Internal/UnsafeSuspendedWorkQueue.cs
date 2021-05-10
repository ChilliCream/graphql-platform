using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing.Plan;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class UnsafeSuspendedWorkQueue
    {
        private readonly List<IExecutionTask> _tasks = new();
        private IExecutionTask? _head;

        public bool IsEmpty { get; private set; } = true;

        public bool CopyTo(UnsafeWorkQueue work, QueryPlanStateMachine stateMachine)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            if (stateMachine == null)
            {
                throw new ArgumentNullException(nameof(stateMachine));
            }

            if (_head is null)
            {
                return false;
            }

            IExecutionTask? head = _head;
            _head = null;
            IsEmpty = true;

            IExecutionTask? tail = null;
            _tasks.Clear();

            while (head is not null)
            {
                IExecutionTask? next = head.Next;

                head.Previous = null;
                head.Next = null;

                if (stateMachine.IsSuspended(head))
                {
                    AppendTask(ref tail, head);
                }
                else
                {
                    _tasks.Add(head);
                }

                head = next;
            }

            var copiedTasks = _tasks.Count > 0;
            work.PushMany(_tasks);
            _tasks.Clear();

            if (tail is null)
            {
                IsEmpty = true;
                return copiedTasks;
            }

            if (_head is null)
            {
                _head = tail;
                IsEmpty = false;
                return copiedTasks;
            }

            if (ReferenceEquals(tail, tail.Next))
            {
                tail.Next = null;
                tail.Previous = null;
                AppendTask(ref _head, tail);
            }

            IExecutionTask mainHead = _head!;
            IExecutionTask mainTail = mainHead.Previous!;
            head = tail.Next!;

            mainHead.Previous = tail;
            tail.Next = mainHead;
            head.Previous = mainTail;
            mainTail.Next = head;

            IsEmpty = false;
            return copiedTasks;
        }

        public void Enqueue(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            AppendTask(ref _head, executionTask);
        }

        private void AppendTask(ref IExecutionTask? head, IExecutionTask executionTask)
        {
            if (head is null)
            {
                head = executionTask;
                head.Next = executionTask;
                head.Previous = executionTask;
            }

            executionTask.Next = head;
            executionTask.Previous = head.Previous;
            head.Previous!.Next = executionTask;
            head.Previous = executionTask;
        }

        public void Clear()
        {
            _head = null;
            IsEmpty = true;
        }
    }
}
