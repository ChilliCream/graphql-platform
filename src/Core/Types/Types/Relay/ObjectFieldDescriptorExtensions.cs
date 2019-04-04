using System.Threading.Tasks;
using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Relay
{
    public static class ObjectFieldDescriptorExtensions
    {
        private static MethodInfo _use =
            typeof(MiddlewareObjectFieldDescriptorExtensions)
            .GetTypeInfo().DeclaredMethods.First(t =>
            {
                if (t.Name.EqualsOrdinal(
                    nameof(MiddlewareObjectFieldDescriptorExtensions.Use))
                    && t.GetGenericArguments().Length == 1)
                {
                    ParameterInfo[] parameters = t.GetParameters();
                    return (parameters.Length == 1
                        && parameters[0].ParameterType ==
                            typeof(IObjectFieldDescriptor));
                }
                return false;
            });

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
            var dependency = TypeDependency.FromSchemaType(typeof(TSchemaType));

            descriptor
                .AddPagingArguments()
                .Type(ConnectionType<TSchemaType>.CreateWithTotalCount())
                .Use(placeholder)
                .Configure(new TypeConfiguration<ObjectFieldDefinition>(
                    ConfigurationKind.Completion,
                    (def, deps) =>
                    {



                    },
                    dependency));

            if (NamedTypeInfoFactory.Default.TryExtractClrType(
                typeof(TSchemaType), out Type clrType))
            {
                Type middlewareType = typeof(QueryableConnectionMiddleware<>)
                    .MakeGenericType(clrType);
                _use.MakeGenericMethod(middlewareType)
                    .Invoke(null, new object[] { descriptor });
            }

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
