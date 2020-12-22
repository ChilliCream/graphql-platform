using System;

namespace StrawberryShake
{
    internal class DelegateObserver<T> : IObserver<IOperationResult<T>> where T : class
    {
        private readonly Action<IOperationResult<T>> _next;

        public DelegateObserver(Action<IOperationResult<T>> next)
        {
            _next = next;
        }

        public void OnNext(IOperationResult<T> value) => _next(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
