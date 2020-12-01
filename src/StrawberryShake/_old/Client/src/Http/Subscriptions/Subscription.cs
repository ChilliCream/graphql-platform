using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public abstract class Subscription
        : IResponseStream
        , ISubscription
    {
        private static MethodInfo _genericNew = typeof(Subscription)
            .GetMethods()
            .Single(t => t.IsStatic && t.IsGenericMethodDefinition);

        public abstract string Id { get; }

        public abstract IOperation Operation { get; }

        public abstract IOperationFormatter OperationFormatter { get; }

        public abstract IResultParser ResultParser { get; }

        public abstract ValueTask OnCompletedAsync(
            CancellationToken cancellationToken);

        public abstract ValueTask OnReceiveResultAsync(
            DataResultMessage message,
            CancellationToken cancellationToken);

        public abstract void OnRegister(Func<Task> unregister);

        protected abstract IAsyncEnumerator<IOperationResult> OnGetAsyncEnumerator(
            CancellationToken cancellationToken);

        IAsyncEnumerator<IOperationResult> IAsyncEnumerable<IOperationResult>.GetAsyncEnumerator(
            CancellationToken cancellationToken)
        {
            return OnGetAsyncEnumerator(cancellationToken);
        }

        public abstract ValueTask DisposeAsync();

        public static Subscription<T> New<T>(
            IOperation<T> operation,
            IOperationFormatter operationFormatter,
            IResultParser parser)
            where T : class =>
            new Subscription<T>(operation, operationFormatter, parser);

        public static Subscription New(
            IOperation operation,
            IOperationFormatter operationFormatter,
            IResultParser parser)
        {
            return (Subscription)_genericNew
                .MakeGenericMethod(new[] { parser.ResultType })
                .Invoke(null, new object[] { operation, operationFormatter, parser })!;
        }
    }
}
