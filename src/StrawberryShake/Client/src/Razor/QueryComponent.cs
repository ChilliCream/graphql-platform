using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace StrawberryShake.Razor
{
    public abstract class DataComponent<TRequest>
        : ComponentBase
        , IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;

        [Inject] protected internal TRequest Request { get; internal set; } = default!;

        public void Subscribe(Func<TRequest, IDisposable> subscribe)
        {
            _subscriptions.Add(subscribe(Request));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    foreach (var subscription in _subscriptions)
                    {
                        subscription.Dispose();
                    }
                }

                _disposed = true;
            }
        }
    }
}
