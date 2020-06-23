using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
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
    }
}
