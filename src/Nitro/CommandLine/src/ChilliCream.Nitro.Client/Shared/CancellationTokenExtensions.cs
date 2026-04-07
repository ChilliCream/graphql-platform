using System.Reactive;
using System.Reactive.Subjects;

namespace ChilliCream.Nitro.Client;

internal static class CancellationTokenExtensions
{
    public static CancellationTokenRegistration Register(
        this CancellationToken cancellationToken,
        ISubject<Unit> stopSignal)
    {
        ArgumentNullException.ThrowIfNull(stopSignal);

        return cancellationToken.Register(
            static state =>
            {
                var signal = (ISubject<Unit>)state!;
                signal.OnNext(Unit.Default);
                signal.OnCompleted();
            },
            stopSignal);
    }
}
