using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HotChocolate.Execution.Processing.Internal
{
    internal sealed class PausedWorkQueue
    {
        private readonly List<IExecutionTask> _tasks = new();
        private SpinLock _lock = new(Debugger.IsAttached);
        private IExecutionTask? _head;

        public bool IsEmpty { get; private set; } = true;

        public bool CopyToUnsafe(
            IQueryPlanStep queryPlanStep,
            Action<IReadOnlyList<IExecutionTask>> pushMany)
        {
            IExecutionTask? head;
            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);

                if (_head is null)
                {
                    return false;
                }

                head = _head;
                _head = null;
                IsEmpty = true;
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }

            IExecutionTask? tail = null;
            _tasks.Clear();

            while (head is not null)
            {
                IExecutionTask? next = head.Next;

                head.Previous = null;
                head.Next = null;

                if (queryPlanStep.IsAllowed(head))
                {
                    _tasks.Add(head);
                }
                else
                {
                    AppendTaskUnsafe(ref tail, head);
                }

                head = next;
            }

            var copiedTasks = _tasks.Count > 0;
            pushMany(_tasks);
            _tasks.Clear();

            if (tail is null)
            {
                IsEmpty = true;
                return copiedTasks;
            }

            try
            {
                _lock.Enter(ref lockTaken);

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
                    AppendTaskUnsafe(ref _head, tail);
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
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        public void Enqueue(IExecutionTask executionTask)
        {
            if (executionTask is null)
            {
                throw new ArgumentNullException(nameof(executionTask));
            }

            var lockTaken = false;

            try
            {
                _lock.Enter(ref lockTaken);
                AppendTaskUnsafe(ref _head, executionTask);
            }
            finally
            {
                if (lockTaken) _lock.Exit(false);
            }
        }

        private void AppendTaskUnsafe(ref IExecutionTask? head, IExecutionTask executionTask)
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

        public void ClearUnsafe()
        {
            _head = null;
            IsEmpty = true;
        }
    }
}
