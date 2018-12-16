using System;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Paging;
using HotChocolate.Utilities;

namespace HotChocolate
{
    public static class ObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor Use<TMiddleware>(
            this IObjectFieldDescriptor descriptor)
            where TMiddleware : class
        {
            return descriptor.Use(
                ClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IObjectFieldDescriptor UsePaging<TSchemaType, TClrType>(
            this IObjectFieldDescriptor descriptor)
            where TSchemaType : INamedOutputType, new()
        {
            return descriptor.Argument("first", a => a.Type<IntType>())
                .Argument("after", a => a.Type<StringType>())
                .Argument("last", a => a.Type<IntType>())
                .Argument("before", a => a.Type<StringType>())
                .Type<ConnectionType<TSchemaType>>()
                .Use<QueryableConnectionMiddleware<TClrType>>();
        }
    }
}
