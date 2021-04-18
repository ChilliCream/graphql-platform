using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Execution.Batching;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddRootType(
            this IRequestExecutorBuilder builder,
            Type rootType,
            OperationType operation)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rootType is null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
        }

        public static IRequestExecutorBuilder AddRootType(
            this IRequestExecutorBuilder builder,
            ObjectType rootType,
            OperationType operation)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rootType is null)
            {
                throw new ArgumentNullException(nameof(rootType));
            }

            return builder.ConfigureSchema(b => b.AddRootType(rootType, operation));
        }

        public static IRequestExecutorBuilder AddQueryType(
            this IRequestExecutorBuilder builder) =>
            AddQueryType(builder, d => d.Name(OperationTypeNames.Query));

        public static IRequestExecutorBuilder AddQueryType(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddQueryType(d =>
            {
                d.Name(OperationTypeNames.Query);
                configure(d);
            }));
        }

        public static IRequestExecutorBuilder AddQueryType<T>(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddQueryType(configure));
        }

        public static IRequestExecutorBuilder AddQueryType(
            this IRequestExecutorBuilder builder,
            Type queryType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (queryType is null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            return builder.ConfigureSchema(b => b.AddQueryType(queryType));
        }

        public static IRequestExecutorBuilder AddQueryType(
            this IRequestExecutorBuilder builder,
            ObjectType queryType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (queryType is null)
            {
                throw new ArgumentNullException(nameof(queryType));
            }

            return builder.ConfigureSchema(b => b.AddQueryType(queryType));
        }

        public static IRequestExecutorBuilder AddQueryType<TQuery>(
            this IRequestExecutorBuilder builder)
            where TQuery : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddQueryType<TQuery>());
        }

        public static IRequestExecutorBuilder AddMutationType(
            this IRequestExecutorBuilder builder) =>
            AddMutationType(builder, d => d.Name(OperationTypeNames.Mutation));

        public static IRequestExecutorBuilder AddMutationType(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddMutationType(d =>
            {
                d.Name(OperationTypeNames.Mutation);
                configure(d);
            }));
        }

        public static IRequestExecutorBuilder AddMutationType<T>(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddMutationType(configure));
        }

        public static IRequestExecutorBuilder AddMutationType(
            this IRequestExecutorBuilder builder,
            Type mutationType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (mutationType is null)
            {
                throw new ArgumentNullException(nameof(mutationType));
            }

            return builder.ConfigureSchema(b => b.AddMutationType(mutationType));
        }

        public static IRequestExecutorBuilder AddMutationType(
            this IRequestExecutorBuilder builder,
            ObjectType mutationType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (mutationType is null)
            {
                throw new ArgumentNullException(nameof(mutationType));
            }

            return builder.ConfigureSchema(b => b.AddMutationType(mutationType));
        }

        public static IRequestExecutorBuilder AddMutationType<TMutation>(
            this IRequestExecutorBuilder builder)
            where TMutation : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddMutationType<TMutation>());
        }

        public static IRequestExecutorBuilder AddSubscriptionType(
            this IRequestExecutorBuilder builder) =>
            AddSubscriptionType(builder, d => d.Name(OperationTypeNames.Subscription));

        public static IRequestExecutorBuilder AddSubscriptionType(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddSubscriptionType(d =>
            {
                d.Name(OperationTypeNames.Subscription);
                configure(d);
            }));
        }

        public static IRequestExecutorBuilder AddSubscriptionType<T>(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddSubscriptionType(configure));
        }

        public static IRequestExecutorBuilder AddSubscriptionType(
            this IRequestExecutorBuilder builder,
            Type subscriptionType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (subscriptionType is null)
            {
                throw new ArgumentNullException(nameof(subscriptionType));
            }

            return builder.ConfigureSchema(b => b.AddSubscriptionType(subscriptionType));
        }

        public static IRequestExecutorBuilder AddSubscriptionType(
            this IRequestExecutorBuilder builder,
            ObjectType subscriptionType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (subscriptionType is null)
            {
                throw new ArgumentNullException(nameof(subscriptionType));
            }

            return builder.ConfigureSchema(b => b.AddSubscriptionType(subscriptionType));
        }

        public static IRequestExecutorBuilder AddSubscriptionType<TSubscription>(
            this IRequestExecutorBuilder builder)
            where TSubscription : class
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddSubscriptionType<TSubscription>());
        }

        public static IRequestExecutorBuilder AddObjectType(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddObjectType(configure));
        }

        public static IRequestExecutorBuilder AddObjectType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddObjectType<T>());
        }

        public static IRequestExecutorBuilder AddObjectType<T>(
            this IRequestExecutorBuilder builder,
            Action<IObjectTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddObjectType(configure));
        }

        public static IRequestExecutorBuilder AddUnionType(
           this IRequestExecutorBuilder builder,
           Action<IUnionTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddUnionType(configure));
        }

        public static IRequestExecutorBuilder AddUnionType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddUnionType<T>());
        }

        public static IRequestExecutorBuilder AddUnionType<T>(
            this IRequestExecutorBuilder builder,
            Action<IUnionTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddUnionType(configure));
        }

        public static IRequestExecutorBuilder AddEnumType(
           this IRequestExecutorBuilder builder,
           Action<IEnumTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddEnumType(configure));
        }

        public static IRequestExecutorBuilder AddEnumType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddEnumType<T>());
        }

        public static IRequestExecutorBuilder AddEnumType<T>(
            this IRequestExecutorBuilder builder,
            Action<IEnumTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddEnumType(configure));
        }

        public static IRequestExecutorBuilder AddInterfaceType(
           this IRequestExecutorBuilder builder,
           Action<IInterfaceTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddInterfaceType(configure));
        }

        public static IRequestExecutorBuilder AddInterfaceType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddInterfaceType<T>());
        }

        public static IRequestExecutorBuilder AddInterfaceType<T>(
            this IRequestExecutorBuilder builder,
            Action<IInterfaceTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddInterfaceType(configure));
        }

        public static IRequestExecutorBuilder AddInputObjectType(
           this IRequestExecutorBuilder builder,
           Action<IInputObjectTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddInputObjectType(configure));
        }

        public static IRequestExecutorBuilder AddInputObjectType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddInputObjectType<T>());
        }

        public static IRequestExecutorBuilder AddInputObjectType<T>(
            this IRequestExecutorBuilder builder,
            Action<IInputObjectTypeDescriptor<T>> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.AddInputObjectType(configure));
        }

        public static IRequestExecutorBuilder AddType(
            this IRequestExecutorBuilder builder,
            Type type)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return builder.ConfigureSchema(b => b.AddType(type));
        }

        public static IRequestExecutorBuilder AddType(
            this IRequestExecutorBuilder builder,
            INamedType namedType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (namedType is null)
            {
                throw new ArgumentNullException(nameof(namedType));
            }

            return builder.ConfigureSchema(b => b.AddType(namedType));
        }

        public static IRequestExecutorBuilder AddType<T>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddType<T>());
        }

        public static IRequestExecutorBuilder AddTypes(
            this IRequestExecutorBuilder builder,
            params INamedType[] types)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return builder.ConfigureSchema(b => b.AddTypes(types));
        }

        public static IRequestExecutorBuilder AddTypes(
            this IRequestExecutorBuilder builder,
            params Type[] types)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            return builder.ConfigureSchema(b => b.AddTypes(types));
        }

        public static IRequestExecutorBuilder AddDirectiveType(
            this IRequestExecutorBuilder builder,
            Type directiveType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (directiveType is null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            return builder.ConfigureSchema(b => b.AddDirectiveType(directiveType));
        }

        public static IRequestExecutorBuilder AddDirectiveType<TDirective>(
            this IRequestExecutorBuilder builder)
            where TDirective : DirectiveType
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddDirectiveType<TDirective>());
        }

        public static IRequestExecutorBuilder AddDirectiveType(
            this IRequestExecutorBuilder builder,
            DirectiveType directiveType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (directiveType is null)
            {
                throw new ArgumentNullException(nameof(directiveType));
            }

            return builder.ConfigureSchema(b => b.AddDirectiveType(directiveType));
        }

        public static IRequestExecutorBuilder SetSchema<TSchema>(
            this IRequestExecutorBuilder builder)
            where TSchema : ISchema
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.SetSchema<TSchema>());
        }

        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            Type schemaType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            return builder.ConfigureSchema(b => b.SetSchema(schemaType));
        }

        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            ISchema schema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return builder.ConfigureSchema(b => b.SetSchema(schema));
        }

        public static IRequestExecutorBuilder SetSchema(
            this IRequestExecutorBuilder builder,
            Action<ISchemaTypeDescriptor> configure)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return builder.ConfigureSchema(b => b.SetSchema(configure));
        }

        public static IRequestExecutorBuilder AddTypeExtension(
            this IRequestExecutorBuilder builder,
            INamedTypeExtension typeExtension)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeExtension is null)
            {
                throw new ArgumentNullException(nameof(typeExtension));
            }

            return builder.ConfigureSchema(b => b.AddType(typeExtension));
        }

        public static IRequestExecutorBuilder AddTypeExtension(
            this IRequestExecutorBuilder builder,
            Type typeExtension)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeExtension is null)
            {
                throw new ArgumentNullException(nameof(typeExtension));
            }

            return builder.ConfigureSchema(b => b.AddType(typeExtension));
        }

        public static IRequestExecutorBuilder AddTypeExtension<TExtension>(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.AddType<TExtension>());
        }

        [Obsolete("Use BindRuntimeType")]
        public static IRequestExecutorBuilder BindClrType<TRuntimeType, TSchemaType>(
            this IRequestExecutorBuilder builder)
            where TSchemaType : INamedType =>
            BindRuntimeType<TRuntimeType, TSchemaType>(builder);

        public static IRequestExecutorBuilder BindRuntimeType<TRuntimeType, TSchemaType>(
            this IRequestExecutorBuilder builder)
            where TSchemaType : INamedType
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.BindClrType<TRuntimeType, TSchemaType>());
        }

        [Obsolete("Use BindRuntimeType")]
        public static IRequestExecutorBuilder BindClrType(
            this IRequestExecutorBuilder builder,
            Type runtimeType,
            Type schemaType) =>
            BindRuntimeType(builder, runtimeType, schemaType);

        public static IRequestExecutorBuilder BindRuntimeType(
            this IRequestExecutorBuilder builder,
            Type runtimeType,
            Type schemaType)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (runtimeType is null)
            {
                throw new ArgumentNullException(nameof(runtimeType));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(schemaType));
            }

            return builder.ConfigureSchema(b => b.BindClrType(runtimeType, schemaType));
        }

        public static IRequestExecutorBuilder AddExportDirectiveType(
            this IRequestExecutorBuilder builder) =>
            builder.AddDirectiveType<ExportDirectiveType>();
    }
}
