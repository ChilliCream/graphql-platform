using System.Runtime.ExceptionServices;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Disposes a source and a lifetime without letting either disposal mask a fault that already
/// happened, or mask each other when both fail.
/// </summary>
internal static class OrderedDisposal
{
    private const string AttachedKey = "GreenDonut.Data.AttachedExceptions";

    /// <summary>
    /// Disposes the source and then the lifetime; if both throw, the source's exception is
    /// rethrown with the lifetime's attached.
    /// </summary>
    /// <param name="disposeSource">
    /// Disposes the source.
    /// </param>
    /// <param name="disposeLifetime">
    /// Disposes the lifetime, or null if there is no lifetime to dispose.
    /// </param>
    public static async ValueTask ReleaseAsync(Func<ValueTask> disposeSource, Func<ValueTask>? disposeLifetime)
    {
        Exception? sourceException = null;

        try
        {
            await disposeSource().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sourceException = ex;
        }

        if (disposeLifetime is not null)
        {
            try
            {
                await disposeLifetime().ConfigureAwait(false);
            }
            catch (Exception ex) when (sourceException is not null)
            {
                Attach(sourceException, ex);
            }
        }

        if (sourceException is not null)
        {
            ExceptionDispatchInfo.Capture(sourceException).Throw();
        }
    }

    /// <summary>
    /// Attaches <paramref name="suppressed"/> to <paramref name="fault"/> so it is not lost, without
    /// replacing <paramref name="fault"/> as the exception a caller observes.
    /// </summary>
    /// <param name="fault">
    /// The exception that must still be the one observed by every caller.
    /// </param>
    /// <param name="suppressed">
    /// The additional exception to attach.
    /// </param>
    public static void Attach(Exception fault, Exception suppressed)
    {
        if (fault.Data[AttachedKey] is List<Exception> attached)
        {
            attached.Add(suppressed);
        }
        else
        {
            fault.Data[AttachedKey] = new List<Exception> { suppressed };
        }
    }

    /// <summary>
    /// Gets the exceptions attached to <paramref name="fault"/> by <see cref="Attach"/>, in the
    /// order they were attached.
    /// </summary>
    /// <param name="fault">
    /// The exception to read attachments from.
    /// </param>
    public static IReadOnlyList<Exception> GetAttached(Exception fault)
        => fault.Data[AttachedKey] as List<Exception> ?? [];
}
