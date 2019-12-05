using System;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate
{
    public static partial class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddQueryType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddQueryType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddQueryType(builder, new ObjectType<T>(configure));
        }

        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            Type type)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return builder.AddRootType(type, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            return builder.AddRootType(queryType, OperationType.Query);
        }

        public static ISchemaBuilder AddQueryType<TQuery>(
            this ISchemaBuilder builder)
            where TQuery : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddRootType(typeof(TQuery), OperationType.Query);
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddMutationType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddMutationType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddMutationType(builder, new ObjectType<T>(configure));
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            Type type)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return builder.AddRootType(type, OperationType.Mutation);
        }

        public static ISchemaBuilder AddMutationType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            return builder.AddRootType(queryType, OperationType.Mutation);
        }

        public static ISchemaBuilder AddMutationType<TMutation>(
            this ISchemaBuilder builder)
            where TMutation : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddRootType(
                typeof(TMutation),
                OperationType.Mutation);
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddSubscriptionType(builder, new ObjectType(configure));
        }

        public static ISchemaBuilder AddSubscriptionType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return AddSubscriptionType(builder, new ObjectType<T>(configure));
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            Type type)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return builder.AddRootType(type, OperationType.Subscription);
        }

        public static ISchemaBuilder AddSubscriptionType(
            this ISchemaBuilder builder,
            ObjectType queryType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (queryType == null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            return builder.AddRootType(queryType, OperationType.Subscription);
        }

        public static ISchemaBuilder AddSubscriptionType<TSubscription>(
            this ISchemaBuilder builder)
            where TSubscription : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddRootType(
                typeof(TSubscription),
                OperationType.Subscription);
        }

        public static ISchemaBuilder AddObjectType(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new ObjectType(configure));
        }

        public static ISchemaBuilder AddObjectType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(new ObjectType<T>());
        }

        public static ISchemaBuilder AddObjectType<T>(
            this ISchemaBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new ObjectType<T>(configure));
        }

        public static ISchemaBuilder AddUnionType(
           this ISchemaBuilder builder,
           Action<IUnionTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new UnionType(configure));
        }

        public static ISchemaBuilder AddUnionType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(new UnionType<T>());
        }

        public static ISchemaBuilder AddUnionType<T>(
            this ISchemaBuilder builder,
            Action<IUnionTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new UnionType<T>(configure));
        }

        public static ISchemaBuilder AddEnumType(
           this ISchemaBuilder builder,
           Action<IEnumTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new EnumType(configure));
        }

        public static ISchemaBuilder AddEnumType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(new EnumType<T>());
        }

        public static ISchemaBuilder AddEnumType<T>(
            this ISchemaBuilder builder,
            Action<IEnumTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new EnumType<T>(configure));
        }

        public static ISchemaBuilder AddInterfaceType(
           this ISchemaBuilder builder,
           Action<IInterfaceTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new InterfaceType(configure));
        }

        public static ISchemaBuilder AddInterfaceType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(new InterfaceType<T>());
        }

        public static ISchemaBuilder AddInterfaceType<T>(
            this ISchemaBuilder builder,
            Action<IInterfaceTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new InterfaceType<T>(configure));
        }

        public static ISchemaBuilder AddInputObjectType(
           this ISchemaBuilder builder,
           Action<IInputObjectTypeDescriptor> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new InputObjectType(configure));
        }

        public static ISchemaBuilder AddInputObjectType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(new InputObjectType<T>());
        }

        public static ISchemaBuilder AddInputObjectType<T>(
            this ISchemaBuilder builder,
            Action<IInputObjectTypeDescriptor<T>> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.AddType(new InputObjectType<T>(configure));
        }

        public static ISchemaBuilder AddType<T>(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddType(typeof(T));
        }

        public static ISchemaBuilder AddTypes(
            this ISchemaBuilder builder,
            params INamedType[] types)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (INamedType type in types)
            {
                builder.AddType(type);
            }
            return builder;
        }

        public static ISchemaBuilder AddTypes(
            this ISchemaBuilder builder,
            params Type[] types)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (Type type in types)
            {
                builder.AddType(type);
            }
            return builder;
        }

        public static ISchemaBuilder AddDirectiveType(
            this ISchemaBuilder builder,
            Type directiveType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (directiveType == null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            if (directiveType == typeof(DirectiveType)
                || (directiveType.IsGenericType
                && directiveType.GetGenericTypeDefinition() ==
                typeof(DirectiveType<>)))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilderExtensions_DirectiveTypeIsBaseType,
                    nameof(directiveType));
            }

            if (!typeof(DirectiveType).IsAssignableFrom(directiveType))
            {
                throw new ArgumentException(
                    TypeResources.SchemaBuilderExtensions_MustBeDirectiveType,
                    nameof(directiveType));
            }

            return builder.AddType(directiveType);
        }

        public static ISchemaBuilder AddDirectiveType<TDirective>(
            this ISchemaBuilder builder)
            where TDirective : DirectiveType
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddDirectiveType(builder, typeof(TDirective));
        }

        public static ISchemaBuilder SetSchema<TSchema>(
            this ISchemaBuilder builder)
            where TSchema : ISchema
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetSchema(typeof(TSchema));
        }

        public static ISchemaBuilder BindClrType<TClrType, TSchemaType>(
            this ISchemaBuilder builder)
            where TSchemaType : INamedType
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.BindClrType(typeof(TClrType), typeof(TSchemaType));
        }
    }
}
