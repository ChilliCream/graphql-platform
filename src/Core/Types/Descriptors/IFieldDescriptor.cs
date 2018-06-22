using System;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public interface IFieldDescriptor
        : IFluent
    {
        IFieldDescriptor SyntaxNode(FieldDefinitionNode syntaxNode);

        IFieldDescriptor Name(string name);

        IFieldDescriptor Description(string description);

        IFieldDescriptor DeprecationReason(string deprecationReason);

        IFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IFieldDescriptor Type(ITypeNode type);

        IFieldDescriptor Argument(string name, Action<IArgumentDescriptor> argument);

        IFieldDescriptor Ignore();

        IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver);

        IFieldDescriptor Resolver(FieldResolverDelegate fieldResolver, Type resultType);
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
            return descriptor
                .Type<NativeType<TResult>>()
                .Resolver((ctx, ct) => fieldResolver(ctx),
                typeof(NativeType<TResult>));
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
            return descriptor.Resolver((ctx, ct) => fieldResolver(),
               typeof(NativeType<TResult>));
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
            return descriptor.Resolver((ctx, ct) => fieldResolver(ctx),
               typeof(NativeType<TResult>));
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
            return descriptor.Resolver((ctx, ct) => fieldResolver(),
                typeof(NativeType<TResult>));
        }
    }
}
