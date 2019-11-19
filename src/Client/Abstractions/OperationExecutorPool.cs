using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StrawberryShake
{
    public class OperationExecutorPool
        : IOperationExecutorPool
    {
        private readonly Dictionary<string, IOperationExecutorFactory> _executorFact;
        private readonly Dictionary<string, IOperationBatchExecutorFactory> _batchExecutorFact;
        private readonly Dictionary<string, IOperationStreamExecutorFactory> _streamExecutorFact;

        private readonly ConcurrentDictionary<string, IOperationExecutor> _executors =
            new ConcurrentDictionary<string, IOperationExecutor>();
        private readonly ConcurrentDictionary<string, IOperationBatchExecutor> _batchExecutors =
            new ConcurrentDictionary<string, IOperationBatchExecutor>();
        private readonly ConcurrentDictionary<string, IOperationStreamExecutor> _streamExecutors =
            new ConcurrentDictionary<string, IOperationStreamExecutor>();

        public OperationExecutorPool(
            IEnumerable<IOperationExecutorFactory> executorFactories,
            IEnumerable<IOperationBatchExecutorFactory> batchExecutorFactories,
            IEnumerable<IOperationStreamExecutorFactory> streamExecutorFactories)
        {
            if (executorFactories is null)
            {
                throw new ArgumentNullException(nameof(executorFactories));
            }

            if (batchExecutorFactories is null)
            {
                throw new ArgumentNullException(nameof(batchExecutorFactories));
            }

            if (streamExecutorFactories is null)
            {
                throw new ArgumentNullException(nameof(streamExecutorFactories));
            }

            _executorFact = executorFactories.ToDictionary(t => t.Name);
            _batchExecutorFact = batchExecutorFactories.ToDictionary(t => t.Name);
            _streamExecutorFact = streamExecutorFactories.ToDictionary(t => t.Name);
        }

        public IOperationExecutor CreateExecutor(string name)
        {
            if (_executors.TryGetValue(name, out IOperationExecutor? executor))
            {
                return executor;
            }

            if (!_executorFact.TryGetValue(name, out IOperationExecutorFactory? fact))
            {
                throw new ArgumentException(
                    $"There is no executor `{name}` registered.",
                    nameof(name));
            }

            return _executors.GetOrAdd(name, n => fact.CreateExecutor());
        }

        public IOperationBatchExecutor CreateBatchExecutor(string name)
        {
            if (_batchExecutors.TryGetValue(name, out IOperationBatchExecutor? executor))
            {
                return executor;
            }

            if (!_batchExecutorFact.TryGetValue(name, out IOperationBatchExecutorFactory? fact))
            {
                throw new ArgumentException(
                    $"There is no batch executor `{name}` registered.",
                    nameof(name));
            }

            return _batchExecutors.GetOrAdd(name, n => fact.CreateBatchExecutor());
        }

        public IOperationStreamExecutor CreateStreamExecutor(string name)
        {
            if (_streamExecutors.TryGetValue(name, out IOperationStreamExecutor? executor))
            {
                return executor;
            }

            if (!_streamExecutorFact.TryGetValue(name, out IOperationStreamExecutorFactory? fact))
            {
                throw new ArgumentException(
                    $"There is no stream executor `{name}` registered.",
                    nameof(name));
            }

            return _streamExecutors.GetOrAdd(name, n => fact.CreateStreamExecutor());
        }
    }
}
