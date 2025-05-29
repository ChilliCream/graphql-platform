using System.Collections.Immutable;

namespace HotChocolate.Utilities;

public static class FireAndForgetTaskExtensions
{
    private static ImmutableArray<BackgroundTaskErrorInterceptor> _interceptors =
        ImmutableArray<BackgroundTaskErrorInterceptor>.Empty;

    public static void FireAndForget(
        this Task task,
        Action? onComplete = null,
        Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        _ = FireAndForgetInternal(task, onComplete, onError);

        static async Task FireAndForgetInternal(
            Task task,
            Action? onComplete,
            Action<Exception>? onError)
        {
            try
            {
                await task.ConfigureAwait(false);
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                var interceptors = _interceptors;
                foreach (var interceptor in interceptors)
                {
                    interceptor.OnError(ex);
                }

                onError?.Invoke(ex);
            }
        }
    }

    public static void FireAndForget(
        this ValueTask task,
        Action? onComplete = null,
        Action<Exception>? onError = null)
    {
        _ = FireAndForgetInternal(task, onComplete, onError);

        static async ValueTask FireAndForgetInternal(
            ValueTask task,
            Action? onComplete,
            Action<Exception>? onError)
        {
            try
            {
                await task.ConfigureAwait(false);
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                var interceptors = _interceptors;
                foreach (var interceptor in interceptors)
                {
                    interceptor.OnError(ex);
                }

                onError?.Invoke(ex);
            }
        }
    }

    public static void FireAndForget(
        this Action action,
        Action? onComplete = null,
        Action<Exception>? onError = null)
    {
        _ = FireAndForgetInternal(
            Task.Run(action),
            onComplete,
            onError);

        static async Task FireAndForgetInternal(
            Task task,
            Action? onComplete,
            Action<Exception>? onError)
        {
            try
            {
                await task.ConfigureAwait(false);
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                var interceptors = _interceptors;
                foreach (var interceptor in interceptors)
                {
                    interceptor.OnError(ex);
                }

                onError?.Invoke(ex);
            }
        }
    }

    internal static void SubscribeToErrors(BackgroundTaskErrorInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        ImmutableInterlocked.Update(ref _interceptors, x => x.Add(interceptor));
    }

    internal static void UnsubscribeFromErrors(BackgroundTaskErrorInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);

        ImmutableInterlocked.Update(ref _interceptors, x => x.Remove(interceptor));
    }
}
