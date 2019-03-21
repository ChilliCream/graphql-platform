using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate
{
    internal static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            Type type)
        {
            return builder.AddRootType(type, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            return builder.AddRootType(queryType, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType<TQuery>(
            this ISchemaBuilder builder)
        {
            return builder.AddRootType(typeof(TQuery), OperationType.Query);
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            Type type)
        {
            return builder.AddRootType(type, OperationType.Mutation);
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            return builder.AddRootType(queryType, OperationType.Mutation);
        }

        public static ISchemaBuilder AddMutationType<TMutation>(
            this ISchemaBuilder builder)
        {
            return builder.AddRootType(
                typeof(TMutation),
                OperationType.Mutation);
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            Type type)
        {
            return builder.AddRootType(type, OperationType.Subscription);
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            return builder.AddRootType(queryType, OperationType.Subscription);
        }

        public static ISchemaBuilder AddSubscriptionType<TSubscription>(
            this ISchemaBuilder builder)
        {
            return builder.AddRootType(
                typeof(TSubscription),
                OperationType.Subscription);
        }

        public static ISchemaBuilder AddType<T>(
            this ISchemaBuilder builder)
        {
            return builder.AddType(typeof(T));
        }
    }
}
