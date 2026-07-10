using System.Diagnostics;

namespace HotChocolate.Diagnostics;

internal abstract class SpanBase(Activity activity, bool shouldDisposeActivity = true) : IDisposable
{
    private bool _disposed;

    public Activity Activity { get; } = activity;

    protected virtual void OnComplete() { }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            OnComplete();

            if (shouldDisposeActivity)
            {
                Activity.Dispose();
            }
        }
    }
}
