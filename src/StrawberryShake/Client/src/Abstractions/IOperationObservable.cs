using System;
using System.Threading;

namespace StrawberryShake
{
    public interface IOperationObservable<T> : IObservable<IOperationResult<T>> where T : class
    {
        void Subscribe(
            Action<IOperationResult<T>> next,
            CancellationToken cancellationToken = default);
    }
}
