using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IObjectFieldDescriptor
        : IFluent
    {
        IObjectFieldDescriptor SyntaxNode(FieldDefinitionNode syntaxNode);

        IObjectFieldDescriptor Name(string name);

        IObjectFieldDescriptor Description(string description);

        IObjectFieldDescriptor DeprecationReason(string deprecationReason);

        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IObjectFieldDescriptor Type(ITypeNode type);

        IObjectFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);

        IObjectFieldDescriptor Ignore();

        IObjectFieldDescriptor Resolver(FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolver(FieldResolverDelegate fieldResolver, Type resultType);
    }

    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, object> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, TResult> fieldResolver)
        {
            return descriptor
                .Type<NativeType<TResult>>()
                .Resolver((ctx, ct) => fieldResolver(ctx),
                typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<object> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver());
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<TResult> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(),
               typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            AsyncFieldResolverDelegate fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx, ct));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<object>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<TResult>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx),
               typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<Task<object>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver());
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<Task<TResult>> fieldResolver)
        {
            return descriptor.Resolver((ctx, ct) => fieldResolver(),
                typeof(NativeType<TResult>));
        }
    }
}
