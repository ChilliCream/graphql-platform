using System;
using System.Threading;
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

        IObjectFieldDescriptor Name(NameString name);

        IObjectFieldDescriptor Description(string description);

        IObjectFieldDescriptor DeprecationReason(string deprecationReason);

        IObjectFieldDescriptor Type<TOutputType>()
            where TOutputType : IOutputType;

        IObjectFieldDescriptor Type(ITypeNode type);

        IObjectFieldDescriptor Argument(NameString name,
            Action<IArgumentDescriptor> argument);

        IObjectFieldDescriptor Ignore();

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver);

        IObjectFieldDescriptor Resolver(
            FieldResolverDelegate fieldResolver,
            Type resultType);

        IObjectFieldDescriptor Use(FieldMiddleware middleware);

        IObjectFieldDescriptor Directive<T>(T directive)
            where T : class;

        IObjectFieldDescriptor Directive<T>()
            where T : class, new();

        IObjectFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);

        IObjectFieldDescriptor Directive(
            string name,
            params ArgumentNode[] arguments);
    }

    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, object> fieldResolver)
        {
            return descriptor.Resolver(ctx =>
                Task.FromResult<object>(fieldResolver(ctx)));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, TResult> fieldResolver)
        {
            return descriptor
                .Type<NativeType<TResult>>()
                .Resolver(ctx => Task.FromResult<object>(fieldResolver(ctx)),
                    typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<object> fieldResolver)
        {
            return descriptor.Resolver(ctx =>
                Task.FromResult<object>(fieldResolver()));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<TResult> fieldResolver)
        {
            return descriptor.Resolver(ctx =>
                Task.FromResult<object>(fieldResolver()),
                typeof(NativeType<TResult>));
        }

        // ? Remove
        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, CancellationToken, object> fieldResolver)
        {
            return descriptor.Resolver(
                ctx => fieldResolver(ctx, ctx.RequestAborted));
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<IResolverContext, Task<TResult>> fieldResolver)
        {
            return descriptor.Resolver(
                async ctx => await fieldResolver(ctx),
                typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Resolver(
            this IObjectFieldDescriptor descriptor,
            Func<Task<object>> fieldResolver)
        {
            return descriptor.Resolver(ctx => fieldResolver());
        }

        public static IObjectFieldDescriptor Resolver<TResult>(
            this IObjectFieldDescriptor descriptor,
            Func<Task<TResult>> fieldResolver)
        {
            return descriptor.Resolver(async ctx => await fieldResolver(),
                typeof(NativeType<TResult>));
        }

        public static IObjectFieldDescriptor Use<TMiddleware>(
            this IObjectFieldDescriptor descriptor)
            where TMiddleware : class
        {
            return descriptor.Use(
                ClassMiddlewareFactory.Create<TMiddleware>());
        }
    }
}
