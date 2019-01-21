using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HotChocolate.Execution.Instrumentation
{
    internal class DiagnosticListenerInitializer
        : IObserver<DiagnosticListener>
        , IDisposable
    {
        private readonly IEnumerable<DiagnosticListener> _listeners;
        private readonly List<IDisposable> _subscriptions;

        public DiagnosticListenerInitializer(
            IEnumerable<DiagnosticListener> listeners)
        {
            _listeners = listeners ??
                throw new ArgumentNullException(nameof(listeners));
            _subscriptions = new List<IDisposable>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (IDisposable subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }
        }

        public void OnCompleted() { /* not required */ }

        public void OnError(Exception error) { /* not required */ }

        public void OnNext(DiagnosticListener value)
        {
            foreach (DiagnosticListener listener in _listeners)
            {
                if (listener.Name == value.Name)
                {
                    _subscriptions.Add(value.SubscribeWithAdapter(listener));
                }
            }
        }

        public void Initialize()
        {
            _subscriptions.Add(
                DiagnosticListener.AllListeners.Subscribe(this));
        }
    }
}
