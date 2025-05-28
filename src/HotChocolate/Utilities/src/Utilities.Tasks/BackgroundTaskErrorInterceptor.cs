namespace HotChocolate.Utilities;

public abstract class BackgroundTaskErrorInterceptor : IDisposable
{
    private bool _disposed;

    protected BackgroundTaskErrorInterceptor()
    {
        FireAndForgetTaskExtensions.SubscribeToErrors(this);
    }

    public abstract void OnError(Exception exception);

    public void Dispose()
    {
        if (!_disposed)
        {
            FireAndForgetTaskExtensions.UnsubscribeFromErrors(this);
            _disposed = true;
        }
    }
}
