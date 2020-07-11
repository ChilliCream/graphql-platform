using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Utilities.Subscriptions;

namespace HotChocolate.Types
{
    public static class SubscribeResolverObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IObservable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IObservable<T> observable = await subscribe(ctx).ConfigureAwait(false);
                return new SourceStreamWrapper(new ObservableSourceStreamAdapter<T>(observable));
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IObservable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IEnumerable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IEnumerable<T> enumerable = await subscribe(ctx).ConfigureAwait(false);
                return new SourceStreamWrapper(new EnumerableSourceStreamAdapter<T>(enumerable));
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IEnumerable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<IAsyncEnumerable<T>>> subscribe)
        {
            return descriptor.Subscribe(async ctx =>
            {
                IAsyncEnumerable<T> enumerable = await subscribe(ctx).ConfigureAwait(false);
                return new SourceStreamWrapper(new AsyncEnumerableStreamAdapter<T>(enumerable));
            });
        }

        public static IObjectFieldDescriptor Subscribe<T>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, IAsyncEnumerable<T>> subscribe) =>
            descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

        /// <summary>
        /// Subscribes to fixed topic on the <see cref="ITopicEventReceiver" />.
        /// </summary>
        /// <param name="descriptor">
        /// The object field descriptor.
        /// </param>
        /// <param name="topicName">
        /// A name representing the topic.
        /// </param>
        /// <typeparam name="TMessage">
        /// The type of the message / event payload.
        /// </typeparam>
        public static IObjectFieldDescriptor SubscribeToTopic<TMessage>(
            this IObjectFieldDescriptor descriptor,
            string topicName) =>
            SubscribeToTopic<string, TMessage>(
                descriptor,
                ctx => topicName);

        /// <summary>
        /// Subscribes to a topic that is represented by an argument value.
        /// </summary>
        /// <param name="descriptor">
        /// The object field descriptor.
        /// </param>
        /// <param name="argumentName">
        /// A name of the argument that is used to resolve the topic.
        /// </param>
        /// <typeparam name="TMessage">
        /// The type of the message / event payload.
        /// </typeparam>
        public static IObjectFieldDescriptor SubscribeToTopic<TTopic, TMessage>(
            this IObjectFieldDescriptor descriptor,
            string argumentName) =>
            SubscribeToTopic<TTopic, TMessage>(
                descriptor,
                ctx => ctx.ArgumentValue<TTopic>(argumentName));

        /// <summary>
        /// Subscribes to a topic that is resolved by executing <paramref name="resolveTopic" />.
        /// </summary>
        /// <param name="descriptor">
        /// The object field descriptor.
        /// </param>
        /// <param name="resolveTopic">
        /// A delegate that resolves a value that will used as topic.
        /// </param>
        /// <typeparam name="TMessage">
        /// The type of the message / event payload.
        /// </typeparam>
        public static IObjectFieldDescriptor SubscribeToTopic<TTopic, TMessage>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, TTopic> resolveTopic) =>
            SubscribeToTopic<TTopic, TMessage>(
                descriptor,
                ctx => new ValueTask<TTopic>(resolveTopic(ctx)));

        /// <summary>
        /// Subscribes to a topic that is resolved by executing <paramref name="resolveTopic" />.
        /// </summary>
        /// <param name="descriptor">
        /// The object field descriptor.
        /// </param>
        /// <param name="resolveTopic">
        /// A delegate that resolves a value that will used as topic.
        /// </param>
        /// <typeparam name="TMessage">
        /// The type of the message / event payload.
        /// </typeparam>
        public static IObjectFieldDescriptor SubscribeToTopic<TTopic, TMessage>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, ValueTask<TTopic>> resolveTopic)
        {
            return descriptor.Subscribe(async ctx =>
            {
                ITopicEventReceiver receiver = ctx.Service<ITopicEventReceiver>();
                TTopic topic = await resolveTopic(ctx).ConfigureAwait(false);
                return await receiver.SubscribeAsync<TTopic, TMessage>(topic).ConfigureAwait(false);
            });
        }
    }
}
