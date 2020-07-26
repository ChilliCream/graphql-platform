using System;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        private static readonly Type _middleware = typeof(ConnectionMiddleware<,>);

        public static IObjectFieldDescriptor UsePaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType =>
            UsePaging<TSchemaType>(descriptor, typeof(TEntity));

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType =>
            UsePaging<TSchemaType>(descriptor, null);

        private static IObjectFieldDescriptor UsePaging<TSchemaType>(
            IObjectFieldDescriptor descriptor,
            Type entityType)
            where TSchemaType : class, IOutputType
        {
            FieldMiddleware placeholder = next => context => Task.CompletedTask;

            descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>()
                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((context, definition) =>
                {
                    if (entityType is null)
                    {
                        var reference = new ClrTypeReference(
                            typeof(TSchemaType),
                            TypeContext.Output);
                        IOutputType type = context.GetType<IOutputType>(reference);
                        entityType = ((IHasClrType)type.NamedType()).ClrType;
                    }

                    MemberInfo member = definition.ResolverMember ?? definition.Member;
                    Type resultType = definition.Resolver is { } && definition.ResultType is { }
                        ? definition.ResultType
                        : member.GetReturnType(true) ?? typeof(object);
                    resultType = UnwrapType(resultType);

                    FieldMiddleware middleware = CreateMiddleware(resultType, entityType);
                    int index = definition.MiddlewareComponents.IndexOf(placeholder);
                    definition.MiddlewareComponents[index] = middleware;
                })
                .DependsOn<TSchemaType>();

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UsePaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType
        {
            descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>();

            return descriptor;
        }

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("first", a => a.Type<PaginationAmountType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<PaginationAmountType>())
                .Argument("before", a => a.Type<StringType>());
        }

        public static IInterfaceFieldDescriptor AddPagingArguments(
            this IInterfaceFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("first", a => a.Type<PaginationAmountType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<PaginationAmountType>())
                .Argument("before", a => a.Type<StringType>());
        }

        private static FieldMiddleware CreateMiddleware(Type sourceType, Type entityType)
        {
            Type middlewareType = _middleware.MakeGenericType(sourceType, entityType);
            return FieldClassMiddlewareFactory.Create(middlewareType);
        }

        internal static Type UnwrapType(Type resultType)
        {
            if (resultType.IsGenericType &&
                resultType.GetGenericTypeDefinition() == typeof(IConnectionResolver<>))
            {
                return resultType.GetGenericArguments()[0];
            }

            if (typeof(IConnectionResolver).IsAssignableFrom(resultType))
            {
                Type[] interfaces = resultType.GetInterfaces();
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type type = interfaces[i];
                    if (type.IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(IConnectionResolver<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                }

                return typeof(object);
            }

            return resultType;
        }
    }
}
