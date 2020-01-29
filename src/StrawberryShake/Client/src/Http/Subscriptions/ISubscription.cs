using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public interface ISubscription
    {
        string Id { get; }

        IOperation Operation { get; }

        IOperationFormatter OperationFormatter { get; }

        IResultParser ResultParser { get; }

        void OnRegister(Func<Task> unregister);

        ValueTask OnReceiveResultAsync(
            DataResultMessage message,
            CancellationToken cancellationToken);

        ValueTask OnCompletedAsync(CancellationToken cancellationToken);
    }
}
