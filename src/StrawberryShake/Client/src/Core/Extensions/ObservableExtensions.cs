using System;

namespace StrawberryShake
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(
            this IObservable<T> observable,
            Action<T> next) =>
            observable.Subscribe(new DelegateObserver<T>(next));
    }
}
