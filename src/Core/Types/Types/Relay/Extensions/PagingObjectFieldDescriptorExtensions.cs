using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Relay
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        private static readonly Type _middleware = typeof(ConnectionMiddleware<>);

        public static IObjectFieldDescriptor UsePaging<TSchemaType, TClrType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType
        {
            return descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>()
                .Use<ConnectionMiddleware<TClrType>>();
        }

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType
        {
            FieldMiddleware placeholder = next => context => Task.CompletedTask;

            descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>()
                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((context, defintion) =>
                {
                    var reference = new ClrTypeReference(typeof(TSchemaType), TypeContext.Output);
                    IOutputType type = context.GetType<IOutputType>(reference);

                    if (type.NamedType() is IHasClrType hasClrType)
                    {
                        FieldMiddleware middleware = CreateMiddleware(hasClrType.ClrType);
                        int index = defintion.MiddlewareComponents.IndexOf(placeholder);
                        defintion.MiddlewareComponents[index] = middleware;
                    }
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

        private static FieldMiddleware CreateMiddleware(Type type)
        {
            if (type.IsGenericType &&
                typeof(IConnectionResolver<>) == type.GetGenericTypeDefinition())
            {
                type = type.GetGenericArguments()[0];
            }

            Type middlewareType = _middleware.MakeGenericType(type);
            return FieldClassMiddlewareFactory.Create(middlewareType);
        }
    }
}
