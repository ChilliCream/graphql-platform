using System;
using StrawberryShake.Impl;

namespace StrawberryShake.Impl
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<TResult>(
            IObservable<IOperationResult<TResult>> observable,
            Action<IOperationResult<TResult>> next)
            where TResult : class =>
            observable.Subscribe(new DelegateObserver<TResult>(next));
    }
}
