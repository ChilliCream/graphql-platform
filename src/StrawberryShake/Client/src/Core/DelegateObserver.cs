using System;

namespace StrawberryShake
{
    internal class DelegateObserver<T> : IObserver<T>
    {
        private readonly Action<T> _next;

        public DelegateObserver(Action<T> next)
        {
            _next = next;
        }

        public void OnNext(T value) => _next(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
