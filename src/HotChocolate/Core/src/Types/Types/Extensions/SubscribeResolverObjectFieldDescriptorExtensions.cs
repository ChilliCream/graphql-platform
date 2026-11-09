#nullable enable

using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using HotChocolate.Utilities;
using HotChocolate.Utilities.StreamAdapters;
using HotChocolate.Utilities.Subscriptions;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// Subscription extensions for <see cref="IObjectFieldDescriptor"/>.
/// </summary>
public static class SubscribeResolverObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<IObservable<TMessage>>> subscribe)
        => descriptor.Subscribe(async ctx =>
        {
            var observable = await subscribe(ctx).ConfigureAwait(false);
            return new SourceStreamWrapper(new ObservableSourceStreamAdapter<TMessage>(observable));
        });

    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, IObservable<TMessage>> subscribe)
        => descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<IEnumerable<TMessage>>> subscribe)
        => descriptor.Subscribe(async ctx =>
        {
            var enumerable = await subscribe(ctx).ConfigureAwait(false);
            return new SourceStreamWrapper(new EnumerableStreamAdapter<TMessage>(enumerable));
        });

    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, IEnumerable<TMessage>> subscribe)
        => descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, Task<IAsyncEnumerable<TMessage>>> subscribe)
        => descriptor.Subscribe(async ctx =>
        {
            var enumerable = await subscribe(ctx).ConfigureAwait(false);
            return new SourceStreamWrapper(new AsyncEnumerableStreamAdapter<TMessage>(enumerable));
        });

    /// <summary>
    /// Subscribes to an event stream.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="subscribe">
    /// The delegate to create the event stream.
    /// </param>
    /// <typeparam name="TMessage">
    /// The type of the message / event payload.
    /// </typeparam>
    public static IObjectFieldDescriptor Subscribe<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, IAsyncEnumerable<TMessage>> subscribe)
        => descriptor.Subscribe(ctx => Task.FromResult(subscribe(ctx)));

    /// <summary>
    /// Subscribes to fixed topic on the <see cref="ITopicEventReceiver" />.
    /// Fields that resolve their topic from argument values cannot subscribe to this topic.
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
        string topicName)
    {
        var feature = SubscriptionTopicFeature.GetOrCreate(descriptor.Extend().Context);
        feature.FixedTopics.Add(topicName);

        return SubscribeToTopic<TMessage>(descriptor, _ => topicName);
    }

    /// <summary>
    /// Subscribes to a topic that is represented by an argument value.
    /// An argument value that is the fixed topic of another subscription field is refused
    /// with a GraphQL error.
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
    public static IObjectFieldDescriptor SubscribeToTopicByArgument<TMessage>(
        this IObjectFieldDescriptor descriptor,
        string argumentName)
    {
        var feature = SubscriptionTopicFeature.GetOrCreate(descriptor.Extend().Context);

        return SubscribeToTopic<TMessage>(
            descriptor,
            ctx =>
            {
                var topicName = ctx.ArgumentValue<object>(argumentName).ToString()!;

                // the topic must not be the fixed topic of another subscription field.
                if (feature.FixedTopics.Contains(topicName))
                {
                    throw ThrowHelper.SubscribeResolver_TopicOfOtherField(ctx.Selection.Field);
                }

                return topicName;
            });
    }

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
    public static IObjectFieldDescriptor SubscribeToTopic<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, string> resolveTopic)
        => SubscribeToTopic<TMessage>(descriptor, ctx => new ValueTask<string>(resolveTopic(ctx)));

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
    public static IObjectFieldDescriptor SubscribeToTopic<TMessage>(
        this IObjectFieldDescriptor descriptor,
        Func<IResolverContext, ValueTask<string>> resolveTopic)
        => descriptor.Subscribe(async ctx =>
        {
            var receiver = ctx.Service<ITopicEventReceiver>();
            var topic = await resolveTopic(ctx).ConfigureAwait(false);
            return await receiver.SubscribeAsync<TMessage>(topic).ConfigureAwait(false);
        });
}
