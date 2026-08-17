namespace Mocha.Transport.AzureServiceBus;

internal static class DisposableExtensions
{
    internal static void DisposeSafe(this IDisposable? disposable)
    {
        try
        {
            disposable?.Dispose();
        }
        catch
        {
        }
    }

    internal static async ValueTask DisposeAsyncSafe(this IAsyncDisposable? disposable)
    {
        if (disposable is null)
        {
            return;
        }

        try
        {
            await disposable.DisposeAsync();
        }
        catch
        {
        }
    }
}
