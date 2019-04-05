using System.Threading.Tasks;
using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Relay
{
    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TSchemaType, TClrType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : IOutputType, new()
        {
            return descriptor
                .AddPagingArguments()
                .Type(ConnectionType<TSchemaType>.CreateWithTotalCount())
                .Use<QueryableConnectionMiddleware<TClrType>>();
        }

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : IOutputType, new()
        {
            FieldMiddleware placeholder =
                next => context => Task.CompletedTask;
            Type middlewareDefinition = typeof(QueryableConnectionMiddleware<>);

            descriptor
                .AddPagingArguments()
                .Type(ConnectionType<TSchemaType>.CreateWithTotalCount())
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

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor)
        {
            return descriptor
                .Argument("first", a => a.Type<PaginationAmountType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<PaginationAmountType>())
                .Argument("before", a => a.Type<StringType>());
        }
    }
}
