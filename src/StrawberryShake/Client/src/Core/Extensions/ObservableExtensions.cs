namespace StrawberryShake.Extensions;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(
        this IObservable<T> observable,
        Action<T> next,
        Action? complete = null) =>
        observable.Subscribe(new DelegateObserver<T>(next, complete));
}
