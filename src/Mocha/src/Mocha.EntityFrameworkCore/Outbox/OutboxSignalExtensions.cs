using System.Runtime.CompilerServices;
using System.Transactions;
using Mocha.Outbox;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxSignalExtensions
{
    private static readonly ConditionalWeakTable<Transaction, IOutboxSignal> s_subscriptions = new();

    /// <summary>
    /// Sets the signal when the ambient <see cref="Transaction"/> completes, or immediately when
    /// there is no ambient transaction. The signal is set at most once per ambient transaction.
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
    {
        if (s_subscriptions.TryGetValue(transaction, out var subscribed) && ReferenceEquals(subscribed, signal))
        {
            return;
        }

        s_subscriptions.AddOrUpdate(transaction, signal);
        transaction.TransactionCompleted += (_, _) => SetUnlessDisposed(signal);
    }

    private static void SetUnlessDisposed(IOutboxSignal signal)
    {
        try
        {
            signal.Set();
        }
        catch (ObjectDisposedException)
        {
            // the outbox worker was shut down before the transaction completed
        }
    }
}
