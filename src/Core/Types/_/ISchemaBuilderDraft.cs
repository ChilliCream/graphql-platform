using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate
{
    public delegate DocumentNode LoadSchemaDocument(IServiceProvider services);




    internal static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            Type type)
        {
            return builder;
            // return builder.AddRootType(type, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            return builder;
            // return builder.AddRootType(queryType, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType<TQuery>(
            this ISchemaBuilder builder)
        {
            return builder;
            // return builder.AddRootType(typeof(TQuery), OperationType.Query);
        }
    }
}
