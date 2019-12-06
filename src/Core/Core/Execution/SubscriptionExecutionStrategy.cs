using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Configuration;
using HotChocolate.Properties;
using HotChocolate.Subscriptions;
using HotChocolate.Language;
using System.Collections.Immutable;
using HotChocolate.Resolvers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution
{
    internal sealed class SubscriptionExecutionStrategy
        : ExecutionStrategyBase
    {
        private readonly IRequestTimeoutOptionsAccessor _options;

        public SubscriptionExecutionStrategy(
            IRequestTimeoutOptionsAccessor options)
        {
            _options = options ??
                throw new ArgumentNullException(nameof(options));
        }

        public override Task<IExecutionResult> ExecuteAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            return ExecuteInternalAsync(executionContext);
        }

        private async Task<IExecutionResult> ExecuteInternalAsync(
            IExecutionContext executionContext)
        {
            FieldSelection fieldSelection = executionContext.CollectFields(
                executionContext.Schema.SubscriptionType,
                executionContext.Operation.Definition.SelectionSet,
                null)
                .Single();

            ImmutableStack<object> source = ImmutableStack<object>.Empty
                .Push(executionContext.Operation.RootValue);

            var subscribeContext = ResolverContext.Rent(
                executionContext,
                fieldSelection,
                source,
                new Dictionary<string, object>());

            FieldResolverDelegate subscribeResolver =
                fieldSelection.Field.SubscribeResolver
                ?? SubscribeDefaultResolverAsync;


            try
            {
                object result = await subscribeResolver(subscribeContext);
                IAsyncEnumerable<object> asyncEnumerable = null;

                if (result is IEventStream a)
                {
                    asyncEnumerable = a.ToSourceStream(executionContext.RequestAborted);
                }
                else if (result is IAsyncEnumerable<object> b)
                {
                    asyncEnumerable = b.ToSourceStream(executionContext.RequestAborted);
                }
                else
                {
                    asyncEnumerable = result.ToSourceStream(executionContext.RequestAborted);
                }

                if (asyncEnumerable is null)
                {
                    throw new QueryException(
                        ErrorBuilder.New()
                            .SetMessage("The event stream is not compatible.")
                            .Build());
                }
            }
            finally
            {
                ResolverContext.Return(subscribeContext);
            }
        }

        private async Task<object> SubscribeDefaultResolverAsync(
            IResolverContext resolverContext)
        {
            EventDescription eventDescription = CreateEvent(executionContext);

            IEventStream eventStream = await SubscribeAsync(
                executionContext.Services, eventDescription)
                    .ConfigureAwait(false);

            return new SubscriptionResult(
                eventStream,
                msg =>
                {
                    IExecutionContext cloned = executionContext.Clone();

                    cloned.ContextData[typeof(IEventMessage).FullName] = msg;

                    return cloned;
                },
                ExecuteSubscriptionQueryAsync,
                executionContext.ServiceScope);
        }

        private static EventDescription CreateEvent(
            IExecutionContext executionContext)
        {
            IReadOnlyCollection<FieldSelection> selections = executionContext
                .CollectFields(
                    executionContext.Operation.RootType,
                    executionContext.Operation.Definition.SelectionSet,
                    null);

            if (selections.Count == 1)
            {
                FieldSelection selection = selections.Single();
                var arguments = new List<ArgumentNode>();
                IVariableValueCollection variables = executionContext.Variables;

                foreach (ArgumentNode argument in selection.Selection.Arguments)
                {
                    if (argument.Value is VariableNode v)
                    {
                        IValueNode value = variables.GetVariable<IValueNode>(v.Name.Value);
                        arguments.Add(argument.WithValue(value));
                    }
                    else
                    {
                        arguments.Add(argument);
                    }
                }

                return new EventDescription(selection.Field.Name, arguments);
            }
            else
            {
                throw new QueryException(CoreResources.Subscriptions_SingleRootField);
            }
        }

        private static ValueTask<IEventStream> SubscribeAsync(
            IServiceProvider services,
            IEventDescription @event)
        {
            var eventRegistry = services.GetService<IEventRegistry>();

            if (eventRegistry == null)
            {
                throw new QueryException(
                    ErrorBuilder.New()
                        .SetMessage(CoreResources.SubscriptionExecutionStrategy_NoEventRegistry)
                        .Build());
            }

            return eventRegistry.SubscribeAsync(@event);
        }

        private async Task<IReadOnlyQueryResult> ExecuteSubscriptionQueryAsync(
            IExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            BatchOperationHandler batchOperationHandler =
                CreateBatchOperationHandler(executionContext);
            var requestTimeoutCts = new CancellationTokenSource(
                _options.ExecutionTimeout);

            try
            {
                using (var combinedCts = CancellationTokenSource
                    .CreateLinkedTokenSource(
                        requestTimeoutCts.Token,
                        cancellationToken))
                {
                    IQueryResult result = await ExecuteQueryAsync(
                        executionContext,
                        batchOperationHandler,
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result.AsReadOnly();
                }
            }
            finally
            {
                batchOperationHandler?.Dispose();
                requestTimeoutCts.Dispose();
            }
        }



    }

    internal static class SourceStreamHelper
    {
        private static readonly MethodInfo _convert = typeof(SourceStreamHelper)
            .GetMethods(BindingFlags.Static)
            .First(m => m.Name == "ToSourceStream" && m.IsGenericMethod);

        private static ConcurrentDictionary<Type, Func<object, IAsyncEnumerable<object>>> _cache =
            new ConcurrentDictionary<Type, Func<object, IAsyncEnumerable<object>>>();

        public static async IAsyncEnumerable<object> ToSourceStream(
            this IEventStream stream,
            [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            await foreach (IEventMessage message in stream.WithCancellation(cancellationToken))
            {
                yield return message;
            }
        }

        public static IAsyncEnumerable<object> ToSourceStream(
            this object stream,
            CancellationToken cancellationToken)
        {
            Type type = stream.GetType();
            Func<object, IAsyncEnumerable<object>> func = _cache.GetOrAdd(type, o =>
            {
                Type enumerable = type.GetInterfaces().FirstOrDefault(t =>
                    t.IsGenericType
                    && t.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

                MethodInfo method = _convert.MakeGenericMethod(
                    enumerable.GetGenericArguments()[0]);

                return new Func<object, IAsyncEnumerable<object>>(o =>
                    (IAsyncEnumerable<object>)method.Invoke(null, new[] { o, cancellationToken }));
            });
            return func?.Invoke(stream);
        }

        public static async IAsyncEnumerable<object> ToSourceStream<T>(
            this IAsyncEnumerable<T> stream,
            [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            await foreach (IEventMessage message in stream.WithCancellation(cancellationToken))
            {
                yield return message;
            }
        }
    }
}
