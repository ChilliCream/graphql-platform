using System.Transactions;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxSignalExtensions
{
    /// <summary>
    /// Sets the signal when the ambient <see cref="Transaction"/> completes, or immediately when
    /// there is no ambient transaction.
    /// </summary>
    /// <param name="signal">The outbox signal to set.</param>
    public static void SetAfterAmbientTransaction(this IOutboxSignal signal)
    {
        if (Transaction.Current is { } transaction)
        {
            SetOnCompletion(signal, transaction);
            return;
        }

        signal.Set();
    }

    private static void SetOnCompletion(IOutboxSignal signal, Transaction transaction)
        => transaction.TransactionCompleted += (_, _) => signal.Set();
}
