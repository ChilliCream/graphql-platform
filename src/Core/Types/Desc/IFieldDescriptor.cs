using System;
using System.ComponentModel;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IFieldDescriptor
        : IFluent
    {
        IFieldDescriptor Name(string name);

        IFieldDescriptor Description(string description);

        IFieldDescriptor DeprecationReason(string deprecationReason);

        IFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        [EditorBrowsable(EditorBrowsableState.Never)]
        IFieldDescriptor Type(Type type, bool overwrite);

        IFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);

        IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver);
    }

    public static class FieldDescriptorExtensions
    {
        public static IFieldDescriptor Resolver(
            this IFieldDescriptor descriptor,
            Func<IResolverContext, object> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IFieldDescriptor Resolver<TResult>(
            this IFieldDescriptor descriptor,
            Func<IResolverContext, TResult> fieldResolver)
        {
            return descriptor.Type(typeof(TResult), false)
                .Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IFieldDescriptor Resolver(
            this IFieldDescriptor descriptor,
            Func<object> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver());
        }

        public static IFieldDescriptor Resolver<TResult>(
            this IFieldDescriptor descriptor,
            Func<TResult> fieldResolver)
        {
            return descriptor.Type(typeof(TResult), false)
                .Resolver((ctx, ct) => fieldResolver());
        }

        public static IFieldDescriptor Resolver(
            this IFieldDescriptor descriptor,
            AsyncFieldResolverDelegate fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx, ct));
        }

        public static IFieldDescriptor Resolver(
            this IFieldDescriptor descriptor,
            Func<IResolverContext, Task<object>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IFieldDescriptor Resolver<TResult>(
            this IFieldDescriptor descriptor,
            Func<IResolverContext, Task<TResult>> fieldResolver)
        {
            return descriptor.Type(typeof(TResult), false)
                .Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IFieldDescriptor Resolver(
            this IFieldDescriptor descriptor,
            Func<Task<object>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver());
        }

        public static IFieldDescriptor Resolver<TResult>(
            this IFieldDescriptor descriptor,
            Func<Task<TResult>> fieldResolver)
        {
            return descriptor.Type(typeof(TResult), false)
                .Resolver((ctx, ct) => fieldResolver());
        }
    }
}
