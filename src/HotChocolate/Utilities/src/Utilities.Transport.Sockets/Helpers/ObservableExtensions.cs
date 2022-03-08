namespace HotChocolate.Utilities.Transport.Sockets.Helpers;

internal static class ObservableExtensions
{
    public static IAsyncEnumerator<TSource> ToAsyncEnumerator<TSource>(
        this IObservable<TSource> observable,
        CancellationToken cancellationToken = default)
        => new GetEnumerator<TSource>(cancellationToken).Run(observable);
}
