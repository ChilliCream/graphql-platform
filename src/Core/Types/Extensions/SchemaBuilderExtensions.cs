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
            Action<IObjectTypeDescriptor> configure)
        {
            return AddQueryType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddQueryType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            return AddQueryType(builder, new ObjectType<T>(configure));
        }

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
            where TQuery : class
        {
            return builder.AddRootType(typeof(TQuery), OperationType.Query);
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            return AddMutationType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddMutationType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            return AddMutationType(builder, new ObjectType<T>(configure));
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
            where TMutation : class
        {
            return builder.AddRootType(
                typeof(TMutation),
                OperationType.Mutation);
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            return AddSubscriptionType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddSubscriptionType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            return AddSubscriptionType(builder, new ObjectType<T>(configure));
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
            where TSubscription : class
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

        public static ISchemaBuilder AddDirectiveType(
            this ISchemaBuilder builder,
            Type directiveType)
        {
            if (directiveType == null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            if (directiveType == typeof(DirectiveType)
                || (directiveType.IsGenericType
                && directiveType.GetGenericTypeDefinition() ==
                typeof(DirectiveType<>)))
            {
                // TODO : resources
                throw new ArgumentException("df", nameof(directiveType));
            }

            if (!typeof(DirectiveType).IsAssignableFrom(directiveType))
            {
                // TODO : resources
                throw new ArgumentException("df", nameof(directiveType));
            }

            return builder.AddType(directiveType);
        }

        public static ISchemaBuilder AddDirectiveType<TDirective>(
            this ISchemaBuilder builder)
            where TDirective : DirectiveType
        {

            return AddDirectiveType(builder, typeof(TDirective));
        }
    }
}
