using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.DataLoader
{
    public sealed class DataLoaderBatchOperation
        : IBatchOperation
        , IObserver<IDataLoader>
    {
        private readonly object _sync = new object();
        private readonly HashSet<IDataLoader> _dataLoader =
            new HashSet<IDataLoader>();

        public int BatchSize => throw new NotImplementedException();

        public event EventHandler<EventArgs> BatchSizeIncreased;

        public Task InvokeAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void OnNext(IDataLoader value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!_dataLoader.Contains(value))
            {
                lock (_sync)
                {
                    if (_dataLoader.Add(value))
                    {
                        // TODO : Register Event
                    }
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
