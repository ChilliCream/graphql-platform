using System;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Relay
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TSchemaType, TClrType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType
        {
            return descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>()
                .Use<QueryableConnectionMiddleware<TClrType>>();
        }

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : class, IOutputType
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;
            Type middlewareDefinition = typeof(QueryableConnectionMiddleware<>);

            descriptor
                .AddPagingArguments()
                .Type<ConnectionWithCountType<TSchemaType>>()
                .Use(placeholder)
                .Extend()
                .OnBeforeCompletion((context, defintion) =>
                {
                    var reference = new ClrTypeReference(
                        typeof(TSchemaType),
                        TypeContext.Output);
                    IOutputType type = context.GetType<IOutputType>(reference);
                    if (type.NamedType() is IHasClrType hasClrType)
                    {
                        Type middlewareType = middlewareDefinition
                            .MakeGenericType(hasClrType.ClrType);
                        FieldMiddleware middleware =
                            FieldClassMiddlewareFactory.Create(middlewareType);
                        int index =
                            defintion.MiddlewareComponents.IndexOf(placeholder);
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
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;
            Type middlewareDefinition = typeof(QueryableConnectionMiddleware<>);

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
    }
}
