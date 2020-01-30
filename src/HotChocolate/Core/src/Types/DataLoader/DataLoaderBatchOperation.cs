using System;
using System.Collections.Immutable;
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

        private ImmutableHashSet<IDataLoader> _touched =
            ImmutableHashSet<IDataLoader>.Empty;

        public int BufferSize => _touched.Count;

        public event EventHandler<EventArgs> BufferedRequests;

        public async Task InvokeAsync(CancellationToken cancellationToken)
        {
            foreach (IDataLoader dataLoader in GetTouchedDataLoaders())
            {
                if (dataLoader.BufferedRequests > 0)
                {
                    await dataLoader.DispatchAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private void RequestBuffered(IDataLoader sender, EventArgs eventArgs)
        {
            if (!_touched.Contains(sender))
            {
                lock (_sync)
                {
                    _touched = _touched.Add(sender);
                }
            }

            BufferedRequests(this, EventArgs.Empty);
        }

        private ImmutableHashSet<IDataLoader> GetTouchedDataLoaders()
        {
            lock (_sync)
            {
                ImmutableHashSet<IDataLoader> touched = _touched;
                _touched = ImmutableHashSet<IDataLoader>.Empty;
                return touched;
            }
        }

        public void OnNext(IDataLoader value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!_touched.Contains(value))
            {
                lock (_sync)
                {
                    if (!_touched.Contains(value))
                    {
                        _touched = _touched.Add(value);
                        value.RequestBuffered += RequestBuffered;
                    }
                }
            }

            BufferedRequests(this, EventArgs.Empty);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}
